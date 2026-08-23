using System;
using System.ComponentModel.DataAnnotations;

namespace Bolcko.Domain.Entities.Content
{
    public class FAQItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "السؤال بالعربية مطلوب")]
        [Display(Name = "السؤال (بالعربية)")]
        public string QuestionAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "الجواب بالعربية مطلوب")]
        [Display(Name = "الجواب (بالعربية)")]
        public string AnswerAr { get; set; } = string.Empty;

        [Display(Name = "السؤال (بالإنجليزية)")]
        public string? QuestionEn { get; set; }

        [Display(Name = "الجواب (بالإنجليزية)")]
        public string? AnswerEn { get; set; }

        [Display(Name = "الصفحة المستهدفة")]
        public string PageTarget { get; set; } = "Calculator";

        [Display(Name = "الترتيب")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "مفعل")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}