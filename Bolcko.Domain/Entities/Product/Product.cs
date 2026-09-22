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
        public bool IsOversized { get; set; } = false; // بضائع ضخمة (بانيوهات، سلالم، حديد، إسمنت)
        public Bolcko.Domain.Enums.SeoStatus SeoStatus { get; set; } = Bolcko.Domain.Enums.SeoStatus.PendingSeo;

        // Supplier Sourcing Mapping
        public string? SupplierKey { get; set; } = "qannas"; // "qannas", "vendor_b"
        public int? ExternalSupplierVariantId { get; set; }  // e.g. 3643 in Qannas API

        public string? TechnicalDatasheetUrl { get; set; }
        public string? MillTestCertificateUrl { get; set; }
        public string? RssApprovalUrl { get; set; }
        public bool IsExclusivePatented { get; set; } = false;
        // Algorithmic Catalog Ranking Boost (SUB-01: Gold 1.5x, Silver 1.2x, Standard 1.0x)
        public double SearchRankingScore { get; set; } = 1.0;

        // Quality Gate & SuperAdmin Catalog Moderation (GOV-01)
        public string ModerationStatus { get; set; } = "Approved"; // "Approved", "PendingReview", "Rejected"
        public string? RejectionReason { get; set; }
        public DateTime? ModeratedAt { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductTierPricing> TierPricings { get; set; } = new List<ProductTierPricing>();
    }
}