using GorevTakip.API.Extensions;
using GorevTakip.API.Middlewares;
using GorevTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

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

// 2. Extension Metotlar ile Temizlenmiş Kayıtlar (Az önce yazdığımız metotlar)
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerConfiguration();

// 3. Varsayılan Ayarlar
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
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

app.UseAuthentication();
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.UseAuthorization();

app.UseCors("AllowAll");
app.MapControllers();

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

//docker-compose stop --> Duraklatmak için
//docker-compose start --> Başlatmak için