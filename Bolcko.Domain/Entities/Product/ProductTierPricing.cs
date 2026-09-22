using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Product
{
    public class ProductTierPricing : BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? TierName { get; set; } // e.g. "تجزئة", "جملة متوسطة", "عطاءات كبرى"
    }
}
