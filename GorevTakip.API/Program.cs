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

// 2. Extension Metotlar ile Temizlenmiş Kayıtlar 
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerConfiguration();

//In-Memory Caching servisini aktif ediyoruz.
builder.Services.AddMemoryCache();

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

app.UseCors("StrictCorsPolicy");
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

/* 
2. Mimari ve Performans İyileştirmeleriDistributed Caching (Redis) Geçişi: 

TaskService.cs içerisinde önbellekleme için IMemoryCache kullanmışsınız. 
Uygulamanızı Docker üzerinde birden fazla instance (container) olarak ayağa kaldırırsanız, her instance'ın kendi RAM'indeki önbellek farklı olacağı için veri tutarsızlıkları yaşanır.  
Geliştirme: Veritabanını PostgreSQL ile Docker'a aldığınız gibi, bir Redis container'ı ekleyerek IDistributedCache yapısına geçiş yapabilirsiniz.


IQueryable Sızıntısının Önlenmesi: 

IGenericRepository içerisinde IQueryable<T> GetQueryable() metodu bulunuyor. 
Bu durum, Business katmanının DataAccess katmanına (ve spesifik olarak EF Core'un sorgu yapısına) çok fazla bağımlı olmasına yol açar.  
Geliştirme: IQueryable dönmek yerine, filtreleme işlemlerini doğrudan repository içinde yapacak spesifik metotlar (örn. ITaskRepository içinde GetTasksWithAssignedUsersAsync) tanımlamak mimariyi daha "Clean" hale getirir.


3. Kullanıcı Deneyimi ve GenişletilebilirlikRefresh Token Mekanizması: 

JWT süresi dolduğunda (mevcut durumda 2 saat) sistem kullanıcıyı dışarı atıyor.  
Geliştirme: Kullanıcı tablosuna RefreshToken ve RefreshTokenExpiryTime kolonları ekleyerek, frontend tarafında Axios (veya mevcut fetch yapınıza bir interceptor) yazarak token süresi dolduğunda kullanıcı hissetmeden arka planda yeni bir token alınmasını sağlayabilirsiniz.


Loglama Altyapısı: 

Hata yönetimi (Exception Middleware) çok güzel kurgulanmış, ancak hatalar şu an sadece konsola yazılıyor.  
Geliştirme: Serilog entegrasyonu yaparak hataları ve sistem akışını Elasticsearch veya doğrudan PostgreSQL üzerinde ayrı bir tabloya yazdırabilirsiniz.
*/