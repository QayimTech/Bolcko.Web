using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Tender.DTOs
{
    public class QuoteRequestItemDto
    {
        public int? ProductId { get; set; }

        [Required(ErrorMessage = "اسم المنتج مطلوب / Product Name is required.")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الكمية مطلوبة / Quantity is required.")]
        [Range(0.001, (double)decimal.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر / Quantity must be greater than 0.")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "الوحدة مطلوبة / Unit is required.")]
        public string Unit { get; set; } = string.Empty;

        public string? Category { get; set; }
        public string? Specifications { get; set; }
    }

    public class QuoteRequestDto
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب / Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم الشركة مطلوب / Company Name is required.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب / Email is required.")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح / Invalid Email Address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب / Phone is required.")]
        [Phone(ErrorMessage = "رقم هاتف غير صالح / Invalid Phone Number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المدينة مطلوبة / City is required.")]
        public string City { get; set; } = string.Empty;

        public string? ProjectName { get; set; }
        public string? ProjectType { get; set; }

        public string? Notes { get; set; }

        public int? TargetVendorId { get; set; }
        public string TenderScope { get; set; } = "OpenNearest"; // "OpenNearest", "DirectVendor", "SubscribedPrivate"
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<QuoteRequestItemDto> Products { get; set; } = new List<QuoteRequestItemDto>();
    }
}
