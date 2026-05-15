using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;

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
            string sessionId = HttpContext.Session.GetString("CartSessionId") ?? Guid.NewGuid().ToString();
            if (HttpContext.Session.GetString("CartSessionId") == null)
            {
                HttpContext.Session.SetString("CartSessionId", sessionId);
            }

            int? userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0") : null;
            var cart = await _serviceManager.ShoppingCartService.GetCartAsync(sessionId, userId);

            return View(cart.TotalItems);
        }
    }
}
