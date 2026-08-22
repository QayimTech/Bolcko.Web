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

        public ICollection<DeliveryBid> Bids { get; set; } = new List<DeliveryBid>();
        public DeliveryRating? Rating { get; set; }
    }
}
