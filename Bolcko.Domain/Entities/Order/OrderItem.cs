using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Product;

namespace Bolcko.Domain.Entities.Order
{
    public class OrderItem : BaseEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public int ProductId { get; set; }
        public Bolcko.Domain.Entities.Product.Product Product { get; set; } = null!;
        public int? ProductVariantId { get; set; }
        public Bolcko.Domain.Entities.Product.ProductVariant? ProductVariant { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
