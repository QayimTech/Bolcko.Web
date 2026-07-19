using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Product;

namespace Bolcko.Domain.Entities.Tender
{
    public class TenderItem : BaseEntity
    {
        public int TenderId { get; set; }
        public Tender Tender { get; set; } = null!;
        public int? ProductId { get; set; }
        public Bolcko.Domain.Entities.Product.Product? Product { get; set; }
        public int? ProductVariantId { get; set; }
        public Bolcko.Domain.Entities.Product.ProductVariant? ProductVariant { get; set; }
        public string? ProductName { get; set; }
        public string? Unit { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal? ProposedPricePerUnit { get; set; }
        public decimal? TargetPricePerUnit { get; set; }
        public decimal? SubtotalItem { get; set; }
    }
}