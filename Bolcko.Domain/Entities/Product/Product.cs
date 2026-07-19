using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Catalog;

namespace Bolcko.Domain.Entities.Product
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public int? SupplierId { get; set; }
        public decimal RetailPrice { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? ImageUrl { get; set; }
        public Bolcko.Domain.Enums.ProductStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool BulkPricingAvailable { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // New properties based on user request
        public string? Brand { get; set; }
        public string? CountryOfOrigin { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}