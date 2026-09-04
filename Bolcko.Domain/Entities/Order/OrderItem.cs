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

        // Sourcing & Multi-Supplier Tracking
        public string? SupplierKey { get; set; } // e.g. "qannas", "vendor_b"
        public int? ExternalSupplierVariantId { get; set; } // e.g. productVariantsId in Qannas (3643)
        public string? ExternalSupplierOrderId { get; set; } // e.g. "3684"
        public Bolcko.Domain.Enums.SourcingStatus SourcingStatus { get; set; } = Bolcko.Domain.Enums.SourcingStatus.PendingSourcing;
        public string? SourcingNotes { get; set; }
    }
}
