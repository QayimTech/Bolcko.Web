using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Tender
{
    public class Tender : BaseEntity
    {
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User User { get; set; } = null!;
        public string TenderTitle { get; set; } = string.Empty;
        public string? TenderDescription { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubmissionDeadline { get; set; }
        public DateTime? RequiredDeliveryDate { get; set; }
        public TenderStatus Status { get; set; }
        public int? AwardedSupplierId { get; set; }
        public decimal? TotalQuotedAmount { get; set; }
        public string? Notes { get; set; }

        public ICollection<TenderItem> Items { get; set; } = new List<TenderItem>();
    }
}