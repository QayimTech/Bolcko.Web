namespace Bolcko.Domain.Entities.Order.DTOs
{
    public class CreateOrderDto
    {
        public int UserId { get; set; }
        public string? Notes { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public AddressDto ShippingAddress { get; set; } = new AddressDto();
        public AddressDto BillingAddress { get; set; } = new AddressDto();
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
