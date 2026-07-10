using System.ComponentModel.DataAnnotations;
using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Setting
{
    public class Coupon : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"

        [Required]
        public decimal DiscountValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        
        public int UsageCount { get; set; } = 0;
    }
}
