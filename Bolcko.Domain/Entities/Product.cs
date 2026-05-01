using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int? SupplierId { get; set; } // Can link to a User with UserType.Company/Contractor
        public decimal RetailPrice { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? ImageUrl { get; set; }
        public ProductStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool BulkPricingAvailable { get; set; }
    }
}