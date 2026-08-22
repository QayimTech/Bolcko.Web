using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "اسم الفئة مطلوب")]
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public string? ParentCategoryNameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
    }
}
