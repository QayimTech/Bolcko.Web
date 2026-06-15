using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Product
{
    public class ProductImage : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }
}
