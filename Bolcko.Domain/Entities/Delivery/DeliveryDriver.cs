using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryDriver : BaseEntity
    {
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User User { get; set; } = null!;

        public int? DeliveryCompanyId { get; set; }
        public DeliveryCompany? Company { get; set; }

        public string? VehicleType { get; set; }
        public string? VehiclePlateNumber { get; set; }
        
        public string? LicenseNumber { get; set; }

        // Is the driver currently available to take jobs?
        public bool IsAvailable { get; set; } = true;

        // Is the driver approved by admin?
        public bool IsApproved { get; set; } = false;

        public decimal AverageRating { get; set; } = 0.0m;
        public int TotalRatings { get; set; } = 0;

        public ICollection<DeliveryJob> Jobs { get; set; } = new List<DeliveryJob>();
        public ICollection<DeliveryBid> Bids { get; set; } = new List<DeliveryBid>();
        public ICollection<DeliveryRating> Ratings { get; set; } = new List<DeliveryRating>();
    }
}
