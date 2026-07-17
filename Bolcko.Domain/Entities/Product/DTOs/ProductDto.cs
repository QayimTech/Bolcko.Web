using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Product.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        [Required(ErrorMessage = "الفئة مطلوبة")]
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SupplierId { get; set; }
        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
        public decimal RetailPrice { get; set; }
        [Required(ErrorMessage = "وحدة القياس مطلوبة")]
        public string UnitOfMeasure { get; set; } = string.Empty;
        [Required(ErrorMessage = "الكمية مطلوبة")]
        public int StockQuantity { get; set; }
        [Required(ErrorMessage = "SKU مطلوب")]
        public string Sku { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool BulkPricingAvailable { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();

        public string? Brand { get; set; }
        public string? CountryOfOrigin { get; set; }
    }
}
