namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class CategoryImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ParentCategoryName { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }

        // To hold the image data extracted from Excel
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
        public string? ImageExtension { get; set; }

        // SEO Metadata from sheet
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }
}
