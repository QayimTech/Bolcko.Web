using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Product;

namespace Bolcko.Domain.Entities.Catalog
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? ImageUrl { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<Bolcko.Domain.Entities.Product.Product> Products { get; set; } = new List<Bolcko.Domain.Entities.Product.Product>();
    }
}