using System;
using System.Threading;
using System.Threading.Tasks;
using GorevTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GorevTakip.API.HostedServices
{
    // BackgroundService sınıfından miras alarak bunun bir arka plan görevi olduğunu belirtiyoruz.
    public class LogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogCleanupService> _logger;

        public LogCleanupService(IServiceProvider serviceProvider, ILogger<LogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Otomatik log temizleme servisi başlatıldı.");

            // stoppingToken iptal edilmediği sürece (uygulama çalıştığı sürece) döngü devam eder
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // KRİTİK NOKTA: BackgroundService'ler "Singleton" (tekil) ömürlüdür.
                    // AppDbContext ise "Scoped" (istek başına) ömürlüdür. 
                    // Singleton bir servisin içine Scoped bir servisi doğrudan enjekte edemeyiz.
                    // Bu yüzden IServiceProvider üzerinden kendimize yeni bir Scope (kapsam) açıyoruz.
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // PostgreSQL'e özel SQL sorgusu: 30 günden daha eski olan kayıtları siler.
                        // Tablo adı "Logs", tarih sütunu Serilog varsayılanı olan "timestamp"tir.
                        var query = "DELETE FROM \"Logs\" WHERE \"timestamp\" < NOW() - INTERVAL '30 days';";
                        
                        var deletedRows = await dbContext.Database.ExecuteSqlRawAsync(query, stoppingToken);
                        
                        if (deletedRows > 0)
                        {
                            _logger.LogInformation($"{deletedRows} adet 30 günden eski log veritabanından temizlendi.");
                        }
                    }
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
                {
                    // Tablo bulunamadı (42P01: relation "Logs" does not exist).
                    // Serilog henüz hiç hata yakalamadığı için "Logs" tablosunu oluşturmamış.
                    // Temizlenecek log olmadığı için bu hatayı görmezden geliyoruz.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Log temizleme işlemi sırasında beklenmeyen bir hata oluştu.");
                }

                // Döngünün tekrar çalışması için 24 saat bekletiyoruz.
                // İsterseniz test etmek için TimeSpan.FromMinutes(1) yaparak 1 dakikada bir çalıştırabilirsiniz.
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}