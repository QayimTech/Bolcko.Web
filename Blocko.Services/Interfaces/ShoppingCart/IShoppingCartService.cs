using Bolcko.Domain.Entities.ShoppingCart.DTOs;

namespace Blocko.Services.Interfaces.ShoppingCart
{
    public interface IShoppingCartService
    {
        Task<ShoppingCartDto> GetCartAsync(string sessionId, int? userId = null);
        Task<ShoppingCartDto> AddToCartAsync(string sessionId, int productId, int quantity, int? userId = null);
        Task<ShoppingCartDto> UpdateCartItemAsync(string sessionId, int itemId, int quantity, int? userId = null);
        Task<ShoppingCartDto> RemoveFromCartAsync(string sessionId, int itemId, int? userId = null);
        Task ClearCartAsync(string sessionId, int? userId = null);
    }
}
