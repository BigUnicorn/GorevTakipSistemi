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

/* 
Soft Delete (Yumuşak Silme) Mekanizması: 
Şu anda DeleteTaskAsync metodu görevi veritabanından tamamen (Remove) siliyor. 
Entity Framework'teki Cascade kuralları gereği, görev silindiğinde ona bağlı olan TaskHistory ve TaskComment kayıtları da uçacaktır. 
Kurumsal sistemlerde veri kaybedilmez. 
TaskItem entity'sine public bool IsDeleted { get; set; } ekleyip, silme işlemini sadece bu bayrağı true yaparak (Soft Delete) güncellemelisin. 
Ayrıca Repository katmanındaki sorgulara !IsDeleted filtresi (Global Query Filter) ekleyebilirsin.


Refresh Token Entegrasyonu: 
Şu an JWT süresi bittiğinde (2 saat) kullanıcı 401 hatası alıyor ve sistem onu doğrudan login sayfasına atıyor. 
Kullanıcı deneyimini artırmak için bir Refresh Token mekanizması ekleyebilir ve arka planda yeni bir token alarak oturumu kesintisiz sürdürebilirsin.


Caching (Önbellekleme): 
dashboard.html açıldığında GetTaskStatistics endpointe istek atılıyor. 
Bu istatistikler her saniye değişmeyen yoğun veritabanı sorguları içeriyor (Count vb.).
.NET içindeki IMemoryCache veya Redis kullanarak bu istatistik verilerini örneğin 1 dakikalığına önbelleğe alarak veritabanı yükünü hafifletebilirsin.


Global Exception Handling Standardı: 
ExceptionMiddleware yazarak harika bir iş çıkarmışsın. 
Bunu bir adım öteye taşıyıp, API standartları olan ProblemDetails formatında (RFC 7807) yanıt dönmesini sağlayabilirsin. 
Bu, frontend tarafında hataları karşılarken çok daha standart bir yapı sunar.


Güvenlik: 
Docker compose dosyasında ve appsettings.json'da veritabanı şifresi ve JWT key'leri açıkça duruyor. 
Bunları .env dosyalarına taşıyıp Docker üzerinden environment variable olarak okumak güvenlik açısından en doğru yaklaşımdır.
*/