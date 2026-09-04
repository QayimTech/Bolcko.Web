using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Order.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? AppliedCouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool HasOversizedItems { get; set; }
        
        // Multi-Supplier Sourcing Tracking
        public string? PrimarySupplierKey { get; set; } = "qannas";
        public string? ExternalSupplierOrderNumber { get; set; }
        public Bolcko.Domain.Enums.SourcingStatus SourcingStatus { get; set; } = Bolcko.Domain.Enums.SourcingStatus.PendingSourcing;
        public string? SourcingNotes { get; set; }

        public AddressDto? ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
