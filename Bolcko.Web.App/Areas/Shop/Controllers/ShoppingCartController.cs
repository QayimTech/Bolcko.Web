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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? productVariantId = null)
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();

            try
            {
                await _shoppingCartService.AddToCartAsync(sessionId, productId, quantity, userId, productVariantId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemAjax(int itemId, int quantity)
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();
            bool removed = false;

            try
            {
                if (quantity < 1)
                {
                    await _shoppingCartService.RemoveFromCartAsync(sessionId, itemId, userId);
                    removed = true;
                }
                else
                {
                    await _shoppingCartService.UpdateCartItemAsync(sessionId, itemId, quantity, userId);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            var cart = await _shoppingCartService.GetCartAsync(sessionId, userId);
            var updatedItem = cart.Items.FirstOrDefault(i => i.Id == itemId);

            return Json(new
            {
                success = true,
                removed,
                itemSubtotal = updatedItem?.TotalPrice ?? 0m,
                cartSubtotal = cart.Subtotal,
                cartTax = cart.Tax,
                cartShipping = cart.Shipping,
                cartTotal = cart.Total,
                totalItems = cart.TotalItems
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int itemId, int quantity)
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();

            try
            {
                if (quantity < 1)
                {
                    await _shoppingCartService.RemoveFromCartAsync(sessionId, itemId, userId);
                }
                else
                {
                    await _shoppingCartService.UpdateCartItemAsync(sessionId, itemId, quantity, userId);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            string sessionId = GetSessionId();
            int? userId = GetUserId();

            try 
            {
                await _shoppingCartService.RemoveFromCartAsync(sessionId, itemId, userId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "Shop", returnUrl = Url.Action(nameof(Checkout)) });
            }

            var cart = await _shoppingCartService.GetCartAsync(GetSessionId(), userId);
            ViewBag.Cart = cart;

            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            ViewBag.ShippingRates = await uow.ShippingRates.GetAllAsync();

            var enableExpressSetting = await uow.AppSettings.GetByKeyAsync("EnableExpressDelivery");
            var feeSetting = await uow.AppSettings.GetByKeyAsync("ExpressDeliveryFee");

            bool enableExpress = enableExpressSetting?.Value?.ToLower() == "true";
            decimal expressFee = decimal.TryParse(feeSetting?.Value, out decimal f) ? f : 5.0m;

            ViewBag.EnableExpressDelivery = enableExpress;
            ViewBag.ExpressDeliveryFee = expressFee;

            return View(new CheckoutDto());
        }

        [HttpGet]
        public async Task<IActionResult> GetShippingFee(string city)
        {
            var cart = await _shoppingCartService.GetCartAsync(GetSessionId(), GetUserId());
            var fee = await _orderService.GetShippingFeeAsync(city ?? string.Empty, cart.HasOversizedItems);
            return Json(new { success = true, rate = fee });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutDto checkoutDto)
        {
            if (!ModelState.IsValid)
            {
                var cartItems = await _shoppingCartService.GetCartAsync(GetSessionId(), GetUserId());
                ViewBag.Cart = cartItems;
                return View("Checkout", checkoutDto);
            }

            string sessionId = GetSessionId();
            int? userId = GetUserId();

            if (!userId.HasValue)
            {
                // Store checkout data in TempData and redirect to login
                TempData["CheckoutData"] = System.Text.Json.JsonSerializer.Serialize(checkoutDto);
                return RedirectToAction("Login", "Account", new { area = "Shop", returnUrl = Url.Action(nameof(PlaceOrder)) });
            }

            var cart = await _shoppingCartService.GetCartAsync(sessionId, userId);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var order = await _orderService.PlaceOrderAsync(userId.Value, cart, checkoutDto);
            await _shoppingCartService.ClearCartAsync(sessionId, userId);

            // Auto-Dispatch to Active Delivery Provider API (GLC / LogesTechs) if enabled and not oversized
            try
            {
                var deliveryApiService = HttpContext.RequestServices.GetService(typeof(Blocko.Services.Interfaces.Delivery.IDeliveryApiService)) as Blocko.Services.Interfaces.Delivery.IDeliveryApiService;
                var uow = HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork)) as Bolcko.Domain.Interfaces.IUnitOfWork;

                var logMsg = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] AutoDispatch Triggered for Order #{order.Id} ({order.OrderNumber})\n";

                if (deliveryApiService == null || uow == null)
                {
                    logMsg += " -> ERROR: deliveryApiService or uow Service is NULL!\n";
                }
                else
                {
                    var fullOrder = await uow.Orders.GetOrderByIdWithItemsAsync(order.Id);
                    var activeConfig = await deliveryApiService.GetActiveConfigAsync();

                    if (activeConfig == null)
                    {
                        logMsg += " -> SKIPPED: GetActiveConfigAsync returned NULL (No active provider in DB)!\n";
                    }
                    else if (fullOrder == null)
                    {
                        logMsg += " -> ERROR: fullOrder is NULL!\n";
                    }
                    else
                    {
                        var hasOversized = fullOrder.Items != null && fullOrder.Items.Any(i => i.Product != null && i.Product.IsOversized);
                        if (hasOversized)
                        {
                            logMsg += " -> SKIPPED: Order contains Heavy/Oversized items!\n";
                        }
                        else
                        {
                            logMsg += $" -> Invoking CreateShipmentAsync for Active Provider ({activeConfig.ProviderKey})...\n";
                            var dispatchRes = await deliveryApiService.CreateShipmentAsync(fullOrder, "", "", "", "تحويل أوتوماتيكي تلقائي عند الطلب عبر المتجر");
                            logMsg += $" -> Result Success={dispatchRes.Success}, Message={dispatchRes.Message}, ErrorDetail={dispatchRes.ErrorDetail}\n";

                            if (dispatchRes.Success)
                            {
                                fullOrder.Status = Bolcko.Domain.Enums.OrderStatus.Processing;
                                await uow.CompleteAsync();
                            }
                        }
                    }
                }
                var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
                System.IO.File.AppendAllText(Path.Combine(logFolder, "delivery_api.log"), logMsg + "===================================\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("logs/delivery_api.log", $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] AutoDispatch EXCEPTION: {ex.Message}\n{ex.StackTrace}\n===================================\n");
            }

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
            // Defensive check - Session should always be available after UseSession() middleware
            if (HttpContext.Session == null)
            {
                // Fallback - generate a temporary ID (though this won't persist)
                return Guid.NewGuid().ToString();
            }

            var existingSessionId = HttpContext.Session.GetString("CartSessionId");
            if (string.IsNullOrEmpty(existingSessionId))
            {
                existingSessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartSessionId", existingSessionId);
            }
            return existingSessionId;
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
