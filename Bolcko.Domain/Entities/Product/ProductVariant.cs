using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Product
{
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string? Size { get; set; } // e.g. "1/2 Inch", "50 mm"
        public string? Color { get; set; } // e.g. "Chrome", "Gold"
        public string? PackagingUnit { get; set; } // e.g. "Piece", "Box"
        public string? CountryOfOrigin { get; set; } // e.g. "Italian", "Spanish"
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
