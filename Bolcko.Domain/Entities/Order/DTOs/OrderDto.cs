using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Order.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
