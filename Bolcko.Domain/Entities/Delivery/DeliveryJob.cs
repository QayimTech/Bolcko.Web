using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryJob : BaseEntity
    {
        public int OrderId { get; set; }
        public Bolcko.Domain.Entities.Order.Order Order { get; set; } = null!;

        public int? DriverId { get; set; }
        public DeliveryDriver? Driver { get; set; }

        public int? DeliveryCompanyId { get; set; }
        public DeliveryCompany? Company { get; set; }

        public DeliveryJobStatus Status { get; set; } = DeliveryJobStatus.Available;

        public decimal DeliveryFee { get; set; }

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;

        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Financial & Reconciliation Fields
        public decimal? CollectedAmount { get; set; }
        public bool IsReconciled { get; set; } = false;
        public DateTime? ReconciledAt { get; set; }
        public string? ReturnReason { get; set; }
        
        // Security token for driver anonymous update link
        public string? DeliveryToken { get; set; }

        // LOG-03 Heavy Transport Load Radar, Weighbridge & e-POD Specifications
        public string? MaterialType { get; set; } // نوع المادة: إسمنت سائب، حديد تسليح، باطون، رمل وحصمة
        public decimal? WeightTons { get; set; } // الوزن الصافي المطلوب نقله (طن)
        public string? WeighbridgeTicketUrl { get; set; } // صورة تذكرة القبان عند بوابة الخروج
        public decimal? GrossWeightTons { get; set; } // الوزن القائم الإجمالي (طن)
        public decimal? TareWeightTons { get; set; } // وزن الشاحنة فارغة (طن)
        public DateTime? WeighedAt { get; set; } // وقت التوزين والخروج
        public string? DeliveryOtpCode { get; set; } // 6-digit OTP code for e-POD
        public bool IsPodVerified { get; set; } = false;
        public DateTime? PodVerifiedAt { get; set; }

        public ICollection<DeliveryBid> Bids { get; set; } = new List<DeliveryBid>();
        public DeliveryRating? Rating { get; set; }
    }
}
