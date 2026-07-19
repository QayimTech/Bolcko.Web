namespace Bolcko.Domain.Entities.ShoppingCart.DTOs
{
    public class ShoppingCartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int? ProductVariantId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? PackagingUnit { get; set; }
        public string? CountryOfOrigin { get; set; }
        public string ProductSku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public int StockQuantity { get; set; }
    }
}
