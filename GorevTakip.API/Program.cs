using GorevTakip.API.Extensions;
using GorevTakip.API.Middlewares;
using GorevTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;
using System.Collections.Generic;
using GorevTakip.API.HostedServices;
using GorevTakip.API.Services;
using GorevTakip.API.Filters;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantı cümlesini alıyoruz
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Log tablosundaki sütunların veri tiplerini ve isimlerini belirliyoruz
var columnWriters = new Dictionary<string, ColumnWriterBase>
{
    { "timestamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
    { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
    { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
    { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
    { "properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) } // Detaylı veriler JSON olarak tutulur
};

// Serilog Yapılandırması
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Microsoft'un gereksiz loglarını gizler
    .Enrich.FromLogContext()
    .WriteTo.Console() // Geliştirme aşamasında terminalde görmeye devam etmek için
    .WriteTo.PostgreSQL(
        connectionString: connectionString,
        tableName: "Logs",
        columnOptions: columnWriters,
        needAutoCreateTable: true,
        // SADECE Error ve daha üstü (Fatal) seviyedeki logları veritabanına yaz:
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error 
    )
    .CreateLogger();

// .NET Core'un varsayılan loglayıcısı yerine Serilog'u kullanmasını söylüyoruz
builder.Host.UseSerilog();

// 1. Veritabanı Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        npgsqlOptionsAction: sqlOptions =>
        {
            // Veritabanı henüz hazır değilse 5 defa, aralarda bekleyerek tekrar bağlanmayı dener.
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

// 2. Extension Metotlar ile Temizlenmiş Kayıtlar 
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerConfiguration();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Health Checks
builder.Services.AddHealthChecks();

// OpenTelemetry (Gözlemlenebilirlik) Yapılandırması
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("GorevTakip.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(opts => 
        {
            var endpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://jaeger:4317";
            opts.Endpoint = new Uri(endpoint);
        })
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
    );

// Redis Distributed Cache Yapılandırması
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Bağlantı bilgisini environment variables / appsettings'den alıyor
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
    // Redis içinde anahtarların karışmaması için bir ön ek (prefix) koyuyoruz
    options.InstanceName = "GorevTakipCache_"; 
});

// 3. Varsayılan Ayarlar
builder.Services.AddControllers(options =>
{
    // Yazdığımız doğrulama filtresini tüm API'ye global olarak uyguluyoruz
    options.Filters.Add<ValidationFilter>(); 
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictCorsPolicy", policy =>
    {
        // appsettings.json'dan izin verilen adresleri dizi olarak okuyoruz
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins) // Sadece listedeki adreslere izin ver
                  .AllowAnyHeader()            // Gelen istek başlıklarına (Authorization, Content-Type vs.) izin ver
                  .AllowAnyMethod()            // GET, POST, PUT, DELETE metotlarına izin ver
                  .AllowCredentials();         // SignalR ve Cookie kullanımı için gerekli
        }
    });
});

// Arka plan log temizleme servisini sisteme ekliyoruz
builder.Services.AddHostedService<LogCleanupService>();

// Outbox Pattern için arka plan servisi
builder.Services.AddHostedService<OutboxBackgroundService>();

// Rate Limiting Ayarları
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5; // 1 dakikada max 5 istek
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // Kuyruğa alma, direkt reddet
    });

    options.AddFixedWindowLimiter("ApiLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100; // 1 dakikada max 100 istek
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Bekleyen migration'ları uygular. Veritabanı yoksa sıfırdan oluşturur.
        context.Database.Migrate(); 
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration işlemi sırasında bir hata oluştu.");
    }
}

// Merkezi Hata Yönetimi Middleware'i
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("StrictCorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("ApiLimit");

app.MapHealthChecks("/api/health");

// Prometheus Metrics Endpoint (Verileri çekmek için)
app.MapPrometheusScrapingEndpoint();

app.MapHub<GorevTakip.API.Hubs.TaskHub>("/taskhub");

app.Run();

// cd ..
// cd GorevTakip.API
// dotnet run
// docker start gorevtakip-postgres pgadmin
// docker stop gorevtakip-postgres pgadmin

//docker-compose up -d --> Başlatmak için
//docker-compose up -d --build --> Kodları güncelleyip başlatmak için
//docker-compose down --> Durdurmak için
//docker-compose down -v --> Durdurmak ve veritabanını silmek için
//docker builder prune -a -f --> Gereksiz docker image'larını silmek için

//docker-compose stop --> Duraklatmak için
//docker-compose start --> Başlatmak için
