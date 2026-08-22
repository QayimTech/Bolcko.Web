using System.ComponentModel.DataAnnotations;

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

        [Required(ErrorMessage = "سعر المتغير مطلوب")]
        [Range(0.001, double.MaxValue, ErrorMessage = "سعر المتغير يجب أن يكون أكبر من صفر")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون صفر أو أكثر")]
        public int StockQuantity { get; set; }

        public string Sku { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
