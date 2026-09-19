using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Bolcko.Web.App.Areas.Shop.Models
{
    public class InvestorJoinViewModel
    {
        [Required(ErrorMessage = "الاسم القانوني للمستثمر أو الكيان مطلوب")]
        [Display(Name = "الاسم الكامل / اسم الشركة الاستثمارية")]
        public string LegalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب للتواصل والـ OTP")]
        [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
        [Display(Name = "رقم الهاتف الجوال")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهوية الوطنية أو السجل التجاري مطلوب")]
        [Display(Name = "رقم الهوية الوطنية / السجل التجاري")]
        public string NationalIdOrCr { get; set; } = string.Empty;

        [Required(ErrorMessage = "تصنيف فئة المستثمر مطلوب")]
        [Display(Name = "تصنيف المستثمر والملاءمة المالية")]
        public string InvestorCategory { get; set; } = "مستثمر فردي مؤهل"; // مستثمر فردي مؤهل, مكتب عائلي (Family Office), صندوق استثماري, شركة تجارية

        [Required(ErrorMessage = "حجم المحفظة الاستثمارية المستهدفة مطلوب")]
        [Display(Name = "حجم المحفظة الاستثمارية المستهدفة")]
        public string TargetPortfolioAmount { get; set; } = "25,000 - 100,000 د.أ";

        [Required(ErrorMessage = "اسم البنك مطلوب للتحويلات")]
        [Display(Name = "اسم البنك")]
        public string BankName { get; set; } = "البنك العربي";

        [Required(ErrorMessage = "رقم الآيبان البنكي (IBAN) مطلوب")]
        [Display(Name = "رقم الآيبان (IBAN) للتحويلات")]
        public string Iban { get; set; } = string.Empty;

        [Required(ErrorMessage = "إقرار مصدر الأموال مطلوب للامتثال لـ AML")]
        [Display(Name = "مصدر الأموال الرئيسي")]
        public string SourceOfFunds { get; set; } = "أرباح أنشطة تجارية واستثمارية";

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور يجب ألا تقل عن 6 خانات")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "صورة الهوية / السجل التجاري (PDF/Image)")]
        public IFormFile? IdentityDocument { get; set; }

        [Display(Name = "شهادة الآيبان البنكي (PDF/Image)")]
        public IFormFile? IbanDocument { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "يجب الموافقة على شروط اتفاقية الوكالة بالاستثمار والامتثال الشرعي")]
        public bool AcceptTerms { get; set; } = true;
    }
}
