using Blocko.Services.Interfaces.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.ShoppingCart
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ShoppingCartDto> GetCartAsync(string sessionId, int? userId = null)
        {
            ShoppingCart? cart;

            if (userId.HasValue)
            {
                cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId.Value);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        SessionId = sessionId,
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(cart);
                    await _unitOfWork.CompleteAsync();
                }
            }
            else
            {
                cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(cart);
                    await _unitOfWork.CompleteAsync();
                }
            }

            return MapToDto(cart);
        }

        public async Task<ShoppingCartDto> AddToCartAsync(string sessionId, int productId, int quantity, int? userId = null)
        {
            var cart = await GetOrCreateCartAsync(sessionId, userId);
            var product = await _unitOfWork.Products.GetByIdAsync(productId);

            if (product == null)
                throw new Exception("Product not found");

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UnitPrice = product.RetailPrice;
            }
            else
            {
                var newItem = new ShoppingCartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.RetailPrice,
                    AddedAt = DateTime.UtcNow
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.CompleteAsync();

            return MapToDto(cart);
        }

        public async Task<ShoppingCartDto> UpdateCartItemAsync(string sessionId, int itemId, int quantity)
        {
            var cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            if (cart == null)
                throw new Exception("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new Exception("Item not found");

            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.CompleteAsync();

            return MapToDto(cart);
        }

        public async Task<ShoppingCartDto> RemoveFromCartAsync(string sessionId, int itemId)
        {
            var cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            if (cart == null)
                throw new Exception("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                cart.Items.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ShoppingCarts.Update(cart);
                await _unitOfWork.CompleteAsync();
            }

            return MapToDto(cart);
        }

        public async Task ClearCartAsync(string sessionId)
        {
            var cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            if (cart != null)
            {
                cart.Items.Clear();
                cart.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ShoppingCarts.Update(cart);
                await _unitOfWork.CompleteAsync();
            }
        }

        private async Task<ShoppingCart> GetOrCreateCartAsync(string sessionId, int? userId = null)
        {
            ShoppingCart? cart;

            if (userId.HasValue)
            {
                cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId.Value);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        SessionId = sessionId,
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(cart);
                    await _unitOfWork.CompleteAsync();
                }
            }
            else
            {
                cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(cart);
                    await _unitOfWork.CompleteAsync();
                }
            }

            return cart;
        }

        private ShoppingCartDto MapToDto(ShoppingCart cart)
        {
            return new ShoppingCartDto
            {
                Id = cart.Id,
                SessionId = cart.SessionId,
                UserId = cart.UserId,
                Items = cart.Items.Select(i => new ShoppingCartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductImage = i.Product?.ImageUrl,
                    ProductSku = i.Product?.Sku ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    StockQuantity = i.Product?.StockQuantity ?? 0
                }).ToList()
            };
        }
    }
}
