using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Order.DTOs
{
    public class CheckoutDto
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Detailed Address")]
        public string DetailedAddress { get; set; } = string.Empty;

        public string? ProjectName { get; set; }
        
        public string? ProjectType { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "COD";

        public string? Country { get; set; }
        
        public string? PostalCode { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? GoogleMapsUrl { get; set; }

        public string? AddressNotes { get; set; }

        public long? SelectedVillageId { get; set; }

        public string? Notes { get; set; }
    }
}
