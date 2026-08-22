using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryRating : BaseEntity
    {
        public int DeliveryJobId { get; set; }
        public DeliveryJob DeliveryJob { get; set; } = null!;

        public int DriverId { get; set; }
        public DeliveryDriver Driver { get; set; } = null!;

        public int CustomerId { get; set; } // Links to AppUser
        public Bolcko.Domain.Entities.User.User Customer { get; set; } = null!;

        // 1 to 5 stars
        public int RatingValue { get; set; }
        
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
