using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryCompany : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CommercialRegister { get; set; }
        
        // Base rate the company charges (can be overridden per job)
        public decimal BaseDeliveryRate { get; set; }

        public bool IsActive { get; set; } = true;
        
        public ICollection<DeliveryDriver> Drivers { get; set; } = new List<DeliveryDriver>();
    }
}
