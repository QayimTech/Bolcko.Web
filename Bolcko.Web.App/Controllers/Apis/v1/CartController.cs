using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Blocko.Services.DTOs.Api;
using Blocko.Services.DTOs.Api.Cart;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.ShoppingCart;
using Bolcko.Domain.Entities.Order.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    [Authorize]
    public class CartController : BaseApiController
    {
        private readonly IShoppingCartService _cartService;
        private readonly IOrderService _orderService;

        public CartController(IShoppingCartService cartService, IOrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                              ?? User.FindFirst("sub") 
                              ?? User.FindFirst(JwtRegisteredClaimNames.NameId);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                return id;
            }
            return null;
        }

        private string GetSessionId()
        {
            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
            {
                return sessionId.ToString();
            }
            var userId = GetUserId();
            return userId?.ToString() ?? "anonymous-mobile-session";
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var sessionId = GetSessionId();
            var cart = await _cartService.GetCartAsync(sessionId, userId);
            return OkResponse(cart);
        }

        [HttpPost("Add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var userId = GetUserId();
            var sessionId = GetSessionId();
            
            var cart = await _cartService.AddToCartAsync(sessionId, request.ProductId, request.Quantity, userId);
            return OkResponse(cart, "Item added to cart successfully");
        }

        [HttpPut("Items/{itemId}")]
        public async Task<IActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemRequestDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.UpdateCartItemAsync(sessionId, itemId, request.Quantity, userId);
            return OkResponse(cart, "Cart item updated successfully");
        }

        [HttpDelete("Items/{itemId}")]
        public async Task<IActionResult> RemoveFromCart(int itemId)
        {
            var userId = GetUserId();
            var sessionId = GetSessionId();

            var cart = await _cartService.RemoveFromCartAsync(sessionId, itemId, userId);
            return OkResponse(cart, "Item removed from cart successfully");
        }

        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto request)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("Invalid checkout data", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());

            var userId = GetUserId();
            if (userId == null)
                return ErrorResponse("User not authenticated", statusCode: 401);

            var sessionId = GetSessionId();
            var cart = await _cartService.GetCartAsync(sessionId, userId);

            if (cart == null || !cart.Items.Any())
                return ErrorResponse("Your cart is empty");

            var order = await _orderService.PlaceOrderAsync(userId.Value, cart, request);
            
            // Clear the cart after successful checkout
            await _cartService.ClearCartAsync(sessionId, userId);

            return OkResponse(order, "Order placed successfully");
        }
    }
}
