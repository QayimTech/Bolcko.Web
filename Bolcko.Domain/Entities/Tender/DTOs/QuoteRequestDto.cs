using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Tender.DTOs
{
    public class QuoteRequestDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        public string? ProjectName { get; set; }
        public string? ProjectType { get; set; }

        public string? Notes { get; set; }
    }
}
