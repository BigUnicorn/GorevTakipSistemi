using System;
using System.Threading;
using System.Threading.Tasks;
using GorevTakip.API.Hubs;
using GorevTakip.DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GorevTakip.API.Services
{
    public class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxBackgroundService> _logger;

        public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxBackgroundService başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox mesajları işlenirken bir hata oluştu.");
                }

                // 5 saniyede bir polling (gecikme)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TaskHub>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // İşlenmemiş mesajları al
            var messages = await outboxRepository.GetUnprocessedMessagesAsync(50);
            
            if (messages.Count == 0) return;

            foreach (var message in messages)
            {
                try
                {
                    // Payload'ı okuyup SignalR üzerinden ilet
                    // Type alanını metod adı olarak, Payload'ı JSON string olarak (veya deserialize ederek) gönderebiliriz.
                    // HubClients'ta nesne beklemesi olabileceği için, eğer JSON string ise client tarafında parse edilmeli veya
                    // burada dinamik obje olarak deserialize edilmeli. 
                    // Ancak bizim kullandığımız SendAsync metodları genelde anonim tip / nesne bekliyordu.
                    // Payload JSON string olduğu için, doğrudan object olarak deserialize edip yolluyoruz:
                    var payloadObject = System.Text.Json.JsonSerializer.Deserialize<object>(message.Payload);

                    if (message.Type == HubConstants.ReceiveNewComment)
                    {
                        var taskId = System.Text.Json.JsonSerializer.Deserialize<int>(message.Payload);
                        await hubContext.Clients.All.SendAsync(message.Type, taskId, cancellationToken: stoppingToken);
                    }
                    else
                    {
                        await hubContext.Clients.All.SendAsync(message.Type, payloadObject, cancellationToken: stoppingToken);
                    }

                    message.ProcessedOnUtc = DateTime.UtcNow;
                    outboxRepository.Update(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox mesajı işlenemedi. Id: {MessageId}", message.Id);
                    message.Error = ex.Message;
                    outboxRepository.Update(message);
                }
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}
