using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Product.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        [Required(ErrorMessage = "الفئة مطلوبة")]
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SupplierId { get; set; }
        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        public decimal RetailPrice { get; set; }
        [Required(ErrorMessage = "وحدة القياس مطلوبة")]
        public string UnitOfMeasure { get; set; } = string.Empty;
        [Required(ErrorMessage = "الكمية مطلوبة")]
        public int StockQuantity { get; set; }
        [Required(ErrorMessage = "SKU مطلوب")]
        public string Sku { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool BulkPricingAvailable { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<ProductTierPricingDto> TierPricings { get; set; } = new();

        public string? Brand { get; set; }
        public string? CountryOfOrigin { get; set; }
        public bool IsOversized { get; set; } = false;
        public Bolcko.Domain.Enums.SeoStatus SeoStatus { get; set; } = Bolcko.Domain.Enums.SeoStatus.PendingSeo;
        public DateTime UpdatedAt { get; set; }

        // Merchant Identity & Attribution (VM-01, VM-02, SUB-01, SUB-02)
        public string SupplierName { get; set; } = "مجموعة القنّاص لمواد البناء";
        public string SupplierSlug { get; set; } = "al-qannas";
        public bool SupplierIsVerified { get; set; } = true;
        public string SupplierBadge { get; set; } = "مورد ذهبي معتمد";
        public string SupplierCity { get; set; } = "عمّان - رأس العين";
        public double SupplierRating { get; set; } = 4.9;
        public int SupplierCompletedOrders { get; set; } = 1420;
        public string SupplierTier { get; set; } = "Gold";
        public string MerchantTypeName { get; set; } = "مصنع مباشر - خالي من وسيط البيع";

        // Engineering Datasheets & Merchant Authority (SUB-02)
        public string? TechnicalDatasheetUrl { get; set; }
        public string? MillTestCertificateUrl { get; set; }
        public string? RssApprovalUrl { get; set; }
        public bool IsExclusivePatented { get; set; } = false;
        public double SearchRankingScore { get; set; } = 1.0;

        // Quality Gate & SuperAdmin Catalog Moderation (GOV-01)
        public string ModerationStatus { get; set; } = "Approved"; // "Approved", "PendingReview", "Rejected"
        public string? RejectionReason { get; set; }
        public DateTime? ModeratedAt { get; set; }

        public decimal DisplayPrice
        {
            get
            {
                if (Variants != null && Variants.Any(v => v.Price > 0))
                {
                    return Variants.Where(v => v.Price > 0).Min(v => v.Price);
                }
                return RetailPrice;
            }
        }
    }
}
