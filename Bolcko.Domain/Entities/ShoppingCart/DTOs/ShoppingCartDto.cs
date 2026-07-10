namespace Bolcko.Domain.Entities.ShoppingCart.DTOs
{
    public class ShoppingCartDto
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public List<ShoppingCartItemDto> Items { get; set; } = new List<ShoppingCartItemDto>();
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.TotalPrice);
        public decimal Tax => Subtotal * 0.15m;
        public decimal Shipping { get; set; } = 5.00m;
        public decimal Total => Subtotal + Tax + Shipping;
    }
}
