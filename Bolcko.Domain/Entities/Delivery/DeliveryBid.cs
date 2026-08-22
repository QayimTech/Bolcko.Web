using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryBid : BaseEntity
    {
        public int DeliveryJobId { get; set; }
        public DeliveryJob DeliveryJob { get; set; } = null!;

        public int DriverId { get; set; }
        public DeliveryDriver Driver { get; set; } = null!;

        public decimal BidAmount { get; set; }
        
        public DeliveryBidStatus Status { get; set; } = DeliveryBidStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
