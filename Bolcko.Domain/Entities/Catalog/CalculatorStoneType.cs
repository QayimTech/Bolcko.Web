using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Catalog
{
    public class CalculatorStoneType : BaseEntity
    {
        public string Code { get; set; } = string.Empty; // e.g., "Natural_Ruwaished", "Natural_Maan", "Riyadh_Yellow"
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal DefaultPricePerM2 { get; set; }
        public string CountryCode { get; set; } = "JO"; // "JO", "SA", "AE", "EG", "GLOBAL"
        public string ColorHex { get; set; } = "#e5d9c5";
        public double Roughness { get; set; } = 0.85;
        public double Metalness { get; set; } = 0.05;
        public string? TextureDiffuseUrl { get; set; }
        public string? TextureBumpUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
