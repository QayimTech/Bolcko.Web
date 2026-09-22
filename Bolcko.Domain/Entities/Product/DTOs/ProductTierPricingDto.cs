namespace Bolcko.Domain.Entities.Product.DTOs
{
    public class ProductTierPricingDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string TierName { get; set; } = string.Empty;
    }
}
