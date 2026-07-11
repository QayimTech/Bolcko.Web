using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bolcko.Web.App.Hubs
{
    // No [Authorize] - allow any connection, check identity inside
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"[SignalR] Client connected: {Context.ConnectionId}. IsAuthenticated: {Context.User?.Identity?.IsAuthenticated}");
            if (Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated)
            {
                // Robust check using ASP.NET Core framework method
                if (Context.User.IsInRole("Admin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Role_Admin");
                }
                if (Context.User.IsInRole("DeliveryDriver"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Role_DeliveryDriver");
                }

                // Fallback scan for role claims
                foreach (var claim in Context.User.Claims)
                {
                    if (claim.Type == System.Security.Claims.ClaimTypes.Role || claim.Type == "role")
                    {
                        var groupName = $"Role_{claim.Value}";
                        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                        _logger.LogInformation($"[SignalR] Added connection {Context.ConnectionId} to group {groupName}");
                    }
                }
                
                var userId = Context.UserIdentifier;
                _logger.LogInformation($"[SignalR] UserIdentifier: {userId}");
            }
            await base.OnConnectedAsync();
        }

        public object GetConnectionStatus()
        {
            var user = Context.User;
            var roles = new System.Collections.Generic.List<string>();
            if (user != null)
            {
                if (user.IsInRole("Admin")) roles.Add("Admin");
                if (user.IsInRole("DeliveryDriver")) roles.Add("DeliveryDriver");
            }
            return new
            {
                IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,
                UserName = user?.Identity?.Name,
                UserId = Context.UserIdentifier,
                Roles = roles
            };
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"[SignalR] Client disconnected: {Context.ConnectionId}. Exception: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
