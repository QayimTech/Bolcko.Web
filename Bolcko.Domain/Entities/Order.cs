using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities
{
    public class Order : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int ShippingAddressId { get; set; }
        public Address ShippingAddress { get; set; } = null!;
        public int BillingAddressId { get; set; }
        public Address BillingAddress { get; set; } = null!;
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
    }
}