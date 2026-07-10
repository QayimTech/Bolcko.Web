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

        [Required]
        public decimal Rate { get; set; }
    }
}
