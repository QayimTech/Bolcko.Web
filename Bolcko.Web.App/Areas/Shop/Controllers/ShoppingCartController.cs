using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using System.Security.Claims;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;

        public ShoppingCartController(IShoppingCartService shoppingCartService)
        {
            _shoppingCartService = shoppingCartService;
        }

        public async Task<IActionResult> Index()
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();

            var cart = await _shoppingCartService.GetCartAsync(sessionId, userId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();

            await _shoppingCartService.AddToCartAsync(sessionId, productId, quantity, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItem(int itemId, int quantity)
        {
            string sessionId = GetSessionId();
            if (quantity < 1)
            {
                return RedirectToAction(nameof(RemoveItem), new { itemId });
            }
            await _shoppingCartService.UpdateCartItemAsync(sessionId, itemId, quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            string sessionId = GetSessionId();
            await _shoppingCartService.RemoveFromCartAsync(sessionId, itemId);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Checkout()
        {
            return View();
        }

        public IActionResult Confirmation()
        {
            return View();
        }

        private string GetSessionId()
        {
            if (HttpContext.Session == null)
            {
                return Guid.NewGuid().ToString();
            }

            if (HttpContext.Session.GetString("CartSessionId") == null)
            {
                HttpContext.Session.SetString("CartSessionId", Guid.NewGuid().ToString());
            }
            return HttpContext.Session.GetString("CartSessionId")!;
        }

        private int? GetUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;

            var claimsPrincipal = User as ClaimsPrincipal;
            var nameIdentifierClaim = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int id))
            {
                return id;
            }

            return null;
        }
    }
}
