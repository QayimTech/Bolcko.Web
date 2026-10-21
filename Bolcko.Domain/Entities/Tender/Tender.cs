using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Tender
{
    public class Tender : BaseEntity
    {
        public int? UserId { get; set; }
        public Bolcko.Domain.Entities.User.User? User { get; set; }
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestCompany { get; set; }
        public string? GuestCity { get; set; }
        public string TenderTitle { get; set; } = string.Empty;
        public string? TenderDescription { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubmissionDeadline { get; set; }
        public DateTime? RequiredDeliveryDate { get; set; }
        public TenderStatus Status { get; set; }
        public int? AwardedSupplierId { get; set; }
        public decimal? TotalQuotedAmount { get; set; }
        public string? Notes { get; set; }
        public string? NegotiationNotes { get; set; }

        public int? TargetVendorId { get; set; }
        public string TenderScope { get; set; } = "OpenNearest"; // "OpenNearest", "DirectVendor", "SubscribedPrivate"
        public string? ProjectCity { get; set; }
        public double? TargetLatitude { get; set; }
        public double? TargetLongitude { get; set; }
        public decimal? DeliveryFee { get; set; }
        public int? EstimatedDeliveryDays { get; set; }
        public int? BidValidityDays { get; set; }
        public string? VendorOfferNotes { get; set; }

        public ICollection<TenderItem> Items { get; set; } = new List<TenderItem>();
    }
}