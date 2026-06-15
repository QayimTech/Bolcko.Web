using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Entities.Order.DTOs;
using Blocko.Services.Interfaces.Order;
using System.Security.Claims;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;

        public ShoppingCartController(IShoppingCartService shoppingCartService, IOrderService orderService)
        {
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
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

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = await _shoppingCartService.GetCartAsync(GetSessionId(), GetUserId());
            ViewBag.Cart = cart;
            return View(new CheckoutDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutDto checkoutDto)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", checkoutDto);
            }

            string sessionId = GetSessionId();
            int? userId = GetUserId();

            if (!userId.HasValue)
            {
                // In a real app, maybe redirect to login or allow guest checkout. For now, assuming logged in user.
                return RedirectToAction("Login", "Account");
            }

            var cart = await _shoppingCartService.GetCartAsync(sessionId, userId);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var order = await _orderService.PlaceOrderAsync(userId.Value, cart, checkoutDto);
            await _shoppingCartService.ClearCartAsync(sessionId);

            return RedirectToAction(nameof(Confirmation), new { orderId = order.Id });
        }

        public async Task<IActionResult> Confirmation(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if(order == null) return NotFound();
            return View(order);
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
