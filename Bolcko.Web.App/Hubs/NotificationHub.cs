using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                foreach (var claim in Context.User.FindAll(System.Security.Claims.ClaimTypes.Role))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{claim.Value}");
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
