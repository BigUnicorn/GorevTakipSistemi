using System.Linq;
using System.Threading.Tasks;
using GorevTakip.DataAccess;
using GorevTakip.Entities;
using Microsoft.EntityFrameworkCore;

namespace GorevTakip.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationsAsync(string message, int? assignedUserId, int? relatedTaskId)
        {
            // Admin kullanıcıların ID'lerini al
            var adminIds = await _context.Users
                .Where(u => u.Role == UserRole.Admin)
                .Select(u => u.Id)
                .ToListAsync();

            var targetUserIds = adminIds.ToHashSet();
            if (assignedUserId.HasValue)
            {
                targetUserIds.Add(assignedUserId.Value);
            }

            var notifications = targetUserIds.Select(userId => new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                RelatedTaskId = relatedTaskId,
                CreatedAt = System.DateTime.UtcNow
            }).ToList();

            if (notifications.Any())
            {
                await _context.Notifications.AddRangeAsync(notifications);
            }
        }
    }
}
