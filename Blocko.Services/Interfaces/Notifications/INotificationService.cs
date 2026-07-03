using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Notifications
{
    public interface INotificationService
    {
        Task SendNotificationToUserAsync(int userId, string title, string message, string? actionUrl = null);
        Task SendNotificationToAllAsync(string title, string message, string? actionUrl = null);
        Task SendNotificationToRoleAsync(string roleName, string title, string message, string? actionUrl = null);
    }
}
