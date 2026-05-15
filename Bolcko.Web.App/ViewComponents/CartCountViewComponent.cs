using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using System.Security.Claims;

namespace Bolcko.Web.App.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly IServiceManager _serviceManager;

        public CartCountViewComponent(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.Session == null)
            {
                return View(0);
            }

            string sessionId = HttpContext.Session.GetString("CartSessionId") ?? Guid.NewGuid().ToString();
            if (HttpContext.Session.GetString("CartSessionId") == null)
            {
                HttpContext.Session.SetString("CartSessionId", sessionId);
            }

            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                var nameIdentifierClaim = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier);
                if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int id))
                {
                    userId = id;
                }
            }

            var cart = await _serviceManager.ShoppingCartService.GetCartAsync(sessionId, userId);
            return View(cart.TotalItems);
        }
    }
}
