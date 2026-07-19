using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.ShoppingCart
{
    public class ShoppingCartItem : BaseEntity
    {
        public int ShoppingCartId { get; set; }
        public ShoppingCart ShoppingCart { get; set; } = null!;
        public int ProductId { get; set; }
        public Bolcko.Domain.Entities.Product.Product Product { get; set; } = null!;
        public int? ProductVariantId { get; set; }
        public Bolcko.Domain.Entities.Product.ProductVariant? ProductVariant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
