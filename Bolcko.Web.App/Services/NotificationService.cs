using System.Threading.Tasks;
using Blocko.Services.Interfaces.Notifications;
using Bolcko.Web.App.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Bolcko.Web.App.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(int userId, string title, string message, string? actionUrl = null)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                ActionUrl = actionUrl
            });
        }

        public async Task SendNotificationToAllAsync(string title, string message, string? actionUrl = null)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                ActionUrl = actionUrl
            });
        }

        public async Task SendNotificationToRoleAsync(string roleName, string title, string message, string? actionUrl = null)
        {
            await _hubContext.Clients.Group($"Role_{roleName}").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                ActionUrl = actionUrl
            });
        }
    }
}
