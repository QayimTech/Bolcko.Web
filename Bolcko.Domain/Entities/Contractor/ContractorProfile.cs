using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Contractor
{
    public class ContractorProfile : BaseEntity
    {
        public int UserId { get; set; }
        public User.User? User { get; set; }

        public string CompanyNameAr { get; set; } = string.Empty;
        public string CompanyNameEn { get; set; } = string.Empty;
        public string CommercialRegistrationNo { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string ClassificationGrade { get; set; } = "الدرجة الأولى - إنشاء أبنية";
        public string JccaMembershipNumber { get; set; } = string.Empty; // رقم عضوية نقابة مقاولي الإنشاءات الأردنيين JCCA
        public string? JccaCertificateDocUrl { get; set; }
        public string? CommercialRegistrationDocUrl { get; set; }
        public string? TaxCertificateDocUrl { get; set; }
        public bool IsJccaVerified { get; set; } = false;
        public string Status { get; set; } = "Active"; // "PendingVerification", "Active", "Suspended"
        public string ContactPersonName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = "عمان";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }
}
