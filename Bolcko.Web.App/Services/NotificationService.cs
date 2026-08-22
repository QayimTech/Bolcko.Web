using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blocko.Persistence;
using Blocko.Services.Interfaces.Notifications;
using Bolcko.Domain.Entities.User;
using Bolcko.Web.App.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace Bolcko.Web.App.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            BlockoDbContext context,
            UserManager<User> userManager)
        {
            _hubContext = hubContext;
            _context = context;
            _userManager = userManager;
        }

        public async Task SendNotificationToUserAsync(int userId, string title, string message, string? actionUrl = null)
        {
            // 1. Save to Database
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Send via SignalR
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                Id = notification.Id,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                CreatedAt = notification.CreatedAt
            });
        }

        public async Task SendNotificationToAllAsync(string title, string message, string? actionUrl = null)
        {
            // Save to database for all users
            var users = _userManager.Users.ToList();
            var notifications = new List<Notification>();
            var now = DateTime.UtcNow;

            foreach (var user in users)
            {
                notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }

            // Send via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                ActionUrl = actionUrl
            });
        }

        public async Task SendNotificationToRoleAsync(string roleName, string title, string message, string? actionUrl = null)
        {
            // 1. Find all users in role
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var notifications = new List<Notification>();
            var now = DateTime.UtcNow;

            foreach (var user in usersInRole)
            {
                notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }

            // 2. Send to group via SignalR
            await _hubContext.Clients.Group($"Role_{roleName}").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                ActionUrl = actionUrl
            });
        }
    }
}
