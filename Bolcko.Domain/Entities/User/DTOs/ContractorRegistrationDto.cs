using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Bolcko.Domain.Entities.User.DTOs
{
    public class ContractorRegistrationDto
    {
        [Required(ErrorMessage = "الاسم التجاري للمؤسسة/الشركة مطلوب")]
        [Display(Name = "اسم المؤسسة / الشركة")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم السجل التجاري مطلوب")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "رقم السجل التجاري يجب أن يتكون من 10 أرقام")]
        [Display(Name = "رقم السجل التجاري")]
        public string CommercialRegistration { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم الضريبي مطلوب")]
        [Display(Name = "الرقم الضريبي (VAT)")]
        public string TaxId { get; set; } = string.Empty;

        [Required(ErrorMessage = "درجة تصنيف المقاولين مطلوبة")]
        [Display(Name = "درجة التصنيف")]
        public string ContractorClass { get; set; } = "الدرجة الثالثة";

        [Required(ErrorMessage = "اسم المفوض بالتوقيع مطلوب")]
        [Display(Name = "اسم المفوض")]
        public string AuthorizedPersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم هاتف المفوض مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        [Display(Name = "رقم هاتف المفوض")]
        public string AuthorizedPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني للعمل")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 خانات")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "المدينة")]
        public string City { get; set; } = "الرياض";

        [Display(Name = "العنوان الوطني")]
        public string? NationalAddress { get; set; }

        [Display(Name = "شهادة السجل التجاري (PDF/Image)")]
        public IFormFile? CrDocument { get; set; }

        [Display(Name = "شهادة التسجيل الضريبي (PDF/Image)")]
        public IFormFile? TaxCertificate { get; set; }
    }
}
