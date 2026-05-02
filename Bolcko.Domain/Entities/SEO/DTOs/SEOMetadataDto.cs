namespace Bolcko.Domain.Entities.SEO.DTOs
{
    public class SEOMetadataDto
    {
        public int Id { get; set; }
        public string PageName { get; set; } = string.Empty;
        public string? PageTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? PageUrl { get; set; }
        public int? PageOrder { get; set; }
    }
}
