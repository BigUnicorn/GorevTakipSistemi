using System.Threading.Tasks;

namespace GorevTakip.Business.Services
{
    public interface INotificationService
    {
        Task CreateNotificationsAsync(string message, int? assignedUserId, int? relatedTaskId);
    }
}
