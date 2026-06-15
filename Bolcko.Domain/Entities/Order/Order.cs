using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Order
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User User { get; set; } = null!;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int ShippingAddressId { get; set; }
        public Address ShippingAddress { get; set; } = null!;
        public int BillingAddressId { get; set; }
        public Address BillingAddress { get; set; } = null!;
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}