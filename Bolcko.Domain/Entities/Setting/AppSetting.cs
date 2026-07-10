using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Setting
{
    public class AppSetting
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
