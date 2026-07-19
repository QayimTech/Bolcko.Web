namespace Bolcko.Domain.Entities.Product.DTOs
{
    public class ProductVariantDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? PackagingUnit { get; set; }
        public string? CountryOfOrigin { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
