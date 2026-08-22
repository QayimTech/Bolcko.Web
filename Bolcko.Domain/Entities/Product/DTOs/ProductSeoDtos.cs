using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Product.DTOs
{
    public class ProductSeoExportDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? Brand { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? ImageAltText { get; set; }
        public SeoStatus SeoStatus { get; set; }
    }

    public class ProductSeoImportDto
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? ImageAltText { get; set; }
        public SeoStatus? SeoStatus { get; set; }
    }

    public class ProductSeoFilterParamsDto
    {
        public int? CategoryId { get; set; }
        public string? Brand { get; set; }
        public SeoStatus? SeoStatus { get; set; }
        public string? Search { get; set; }
        public bool OnlyMissingDescription { get; set; } = false;
        public List<int>? SelectedProductIds { get; set; }
    }

    public class BulkSeoJobResultDto
    {
        public string JobId { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsCompleted { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
