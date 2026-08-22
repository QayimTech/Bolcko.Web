using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Setting
{
    public class ShippingRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CityNameEn { get; set; }

        [Required]
        public decimal Rate { get; set; }
    }
}
