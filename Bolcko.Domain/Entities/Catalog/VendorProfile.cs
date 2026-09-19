using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Catalog
{
    public class VendorProfile : BaseEntity
    {
        public int UserId { get; set; }
        public User.User? User { get; set; }

        public string CompanyNameAr { get; set; } = string.Empty;
        public string CompanyNameEn { get; set; } = string.Empty;
        public string CommercialRegistrationNo { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "JO"; // "JO", "SA", "AE", etc.
        public string City { get; set; } = string.Empty;
        public string AddressText { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;

        // Specialty Categories (Comma-separated e.g., "Stone,ReadyMix,Steel,Insulation,Finishing")
        public string SuppliedCategories { get; set; } = "Stone";

        // JSON array of covered cities or "All"
        public string CoverageAreasJson { get; set; } = "[\"All\"]";

        // Status: "PendingVerification", "Active", "Suspended", "Rejected"
        public string Status { get; set; } = "PendingVerification";
        public string? VerificationNotes { get; set; }
        public decimal CommissionRatePercentage { get; set; } = 2.0m;
        public double Rating { get; set; } = 5.0;

        // Merchant Authority & Tier (SUB-01 / SUB-02)
        public Bolcko.Domain.Enums.MerchantType MerchantType { get; set; } = Bolcko.Domain.Enums.MerchantType.Manufacturer;
        public string SubscriptionTier { get; set; } = "Standard"; // "Standard", "Silver", "Gold"
        public bool IsGoldVerified { get; set; } = false;
        public bool IsExclusiveAgent { get; set; } = false;

        // Warehouse & Dispatch Logistics Geolocation (MGR-01)
        public double Latitude { get; set; } = 31.9392; // Ras Al-Ain Central Amman Hub
        public double Longitude { get; set; } = 35.9189;
        public string WarehouseLocationName { get; set; } = "مستودعات رأس العين - عمان المركزية";

        // Official KYC & Engineering Datasheets (SUB-02)
        public string? CommercialRegistrationDocUrl { get; set; }
        public string? VocationalLicenseDocUrl { get; set; }
        public string? TaxCertificateDocUrl { get; set; }
        public string? QualityCertificatesDocUrl { get; set; }
        public string? TechnicalDatasheetUrl { get; set; }
        public string? MillTestCertificateUrl { get; set; }
        public string? RssApprovalUrl { get; set; } // شهادة الجمعية العلمية الملكية

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }
}
