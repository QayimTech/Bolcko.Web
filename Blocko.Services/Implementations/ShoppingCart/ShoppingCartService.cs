using Blocko.Services.Interfaces.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.shoppingCart
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
                    // Fallback to session
                    cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
                    if (cart != null && cart.UserId == 0)
                    {
                        cart.UserId = userId.Value;
                        _unitOfWork.ShoppingCarts.Update(cart);
                        await _unitOfWork.CompleteAsync();
                    }
                }
                
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

        public async Task<ShoppingCartDto> AddToCartAsync(string sessionId, int productId, int quantity, int? userId = null, int? productVariantId = null)
        {
            var cart = await GetOrCreateCartAsync(sessionId, userId);
            var product = await _unitOfWork.Products.GetByIdAsync(productId);

            if (product == null)
                throw new Exception("Product not found");

            // Resolve price and stock from variant if provided
            decimal unitPrice = product.RetailPrice;
            int availableStock = product.StockQuantity;

            Bolcko.Domain.Entities.Product.ProductVariant? variant = null;
            if (productVariantId.HasValue)
            {
                variant = await _unitOfWork.ProductVariants.GetByIdAsync(productVariantId.Value);
                if (variant != null)
                {
                    unitPrice = variant.Price;
                    availableStock = variant.StockQuantity;
                }
            }

            if (quantity > availableStock)
                throw new Exception("Not enough stock available");

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.ProductVariantId == productVariantId);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > availableStock)
                    throw new Exception("Not enough stock available");

                existingItem.Quantity += quantity;
                existingItem.UnitPrice = unitPrice;
            }
            else
            {
                var newItem = new ShoppingCartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductId = productId,
                    ProductVariantId = productVariantId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    AddedAt = DateTime.UtcNow
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.CompleteAsync();

            return MapToDto(cart);
        }

        public async Task<ShoppingCartDto> UpdateCartItemAsync(string sessionId, int itemId, int quantity, int? userId = null)
        {
            ShoppingCart? cart = null;
            if (userId.HasValue) 
            {
                cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId.Value);
                if (cart == null) cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            else 
            {
                cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            
            if (cart == null)
                throw new Exception("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new Exception("Item not found");

            if (quantity <= 0)
            {
                cart.Items.Remove(item);
                _unitOfWork.ShoppingCartItems.Remove(item);
            }
            else
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null && quantity > product.StockQuantity)
                    throw new Exception("Not enough stock available");
                    
                item.Quantity = quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ShoppingCarts.Update(cart);
            await _unitOfWork.CompleteAsync();

            return MapToDto(cart);
        }

        public async Task<ShoppingCartDto> RemoveFromCartAsync(string sessionId, int itemId, int? userId = null)
        {
            ShoppingCart? cart = null;
            if (userId.HasValue) 
            {
                cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId.Value);
                if (cart == null) cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            else 
            {
                cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            
            if (cart == null)
                throw new Exception("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                cart.Items.Remove(item);
                _unitOfWork.ShoppingCartItems.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ShoppingCarts.Update(cart);
                await _unitOfWork.CompleteAsync();
            }

            return MapToDto(cart);
        }

        public async Task ClearCartAsync(string sessionId, int? userId = null)
        {
            ShoppingCart? cart = null;
            if (userId.HasValue) 
            {
                cart = await _unitOfWork.ShoppingCarts.GetByUserIdAsync(userId.Value);
                if (cart == null) cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            else 
            {
                cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
            }
            
            if (cart != null)
            {
                var itemsToRemove = cart.Items.ToList();
                cart.Items.Clear();
                foreach (var item in itemsToRemove)
                {
                    _unitOfWork.ShoppingCartItems.Remove(item);
                }
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
                    cart = await _unitOfWork.ShoppingCarts.GetBySessionIdAsync(sessionId);
                    if (cart != null && cart.UserId == 0)
                    {
                        cart.UserId = userId.Value;
                        _unitOfWork.ShoppingCarts.Update(cart);
                        await _unitOfWork.CompleteAsync();
                    }
                }
                
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
            decimal feeVal = 5.00m;
            try
            {
                var setting = _unitOfWork.AppSettings.GetByKeyAsync("ShippingFee").GetAwaiter().GetResult();
                if (setting != null && decimal.TryParse(setting.Value, out decimal parsed))
                {
                    feeVal = parsed;
                }
            }
            catch { }

            return new ShoppingCartDto
            {
                Id = cart.Id,
                SessionId = cart.SessionId,
                UserId = cart.UserId,
                Shipping = feeVal,
                Items = cart.Items.Select(i => new ShoppingCartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductImage = i.ProductVariant != null && !string.IsNullOrEmpty(i.ProductVariant.ImageUrl) ? i.ProductVariant.ImageUrl : i.Product?.ImageUrl,
                    ProductSku = i.ProductVariant != null && !string.IsNullOrEmpty(i.ProductVariant.Sku) ? i.ProductVariant.Sku : (i.Product?.Sku ?? string.Empty),
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    StockQuantity = i.ProductVariant != null ? i.ProductVariant.StockQuantity : (i.Product?.StockQuantity ?? 0),
                    ProductVariantId = i.ProductVariantId,
                    Size = i.ProductVariant?.Size,
                    Color = i.ProductVariant?.Color,
                    PackagingUnit = i.ProductVariant?.PackagingUnit,
                    CountryOfOrigin = i.ProductVariant?.CountryOfOrigin
                }).ToList()
            };
        }
    }
}
