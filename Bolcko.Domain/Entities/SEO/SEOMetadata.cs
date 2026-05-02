using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.SEO
{
    public class SEOMetadata
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string PageName { get; set; } = string.Empty;
        public string? PageTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? PageUrl { get; set; }
        public int? PageOrder { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
