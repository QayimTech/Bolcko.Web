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

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }
}
