using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class VendorRegistrationDto
    {
        [Required(ErrorMessage = "اسم الشركة أو المصنع بالعربية مطلوب")]
        [Display(Name = "اسم الشركة / المصنع (بالعربية)")]
        public string CompanyNameAr { get; set; } = string.Empty;

        [Display(Name = "اسم الشركة / المصنع (بالإنجليزية)")]
        public string? CompanyNameEn { get; set; }

        [Required(ErrorMessage = "الاسم الكامل للشخص المسؤول مطلوب")]
        [Display(Name = "اسم الشخص المسؤول")]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني الرسمي")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الواتساب للمبيعات مطلوب")]
        [Display(Name = "رقم الواتساب الرسمي")]
        public string WhatsApp { get; set; } = string.Empty;

        [Required(ErrorMessage = "الدولة مطلوبة")]
        [Display(Name = "الدولة")]
        public string CountryCode { get; set; } = "JO";

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [Display(Name = "المدينة / المحافظة")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "العنوان التفصيلي وموقع المصنع / المعرض")]
        public string? AddressText { get; set; }

        [Required(ErrorMessage = "رقم السجل التجاري مطلوب")]
        [Display(Name = "رقم السجل التجاري")]
        public string CommercialRegistrationNo { get; set; } = string.Empty;

        [Display(Name = "الرقم الضريبي (إن وجد)")]
        public string? TaxNumber { get; set; }

        [Required(ErrorMessage = "يرجى تحديد مادة واحدة على الأقل من المواد الموردة")]
        [Display(Name = "المواد الإنشائية الموردة")]
        public List<string> SuppliedCategories { get; set; } = new List<string>();

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن لا تقل عن 6 خانات")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
