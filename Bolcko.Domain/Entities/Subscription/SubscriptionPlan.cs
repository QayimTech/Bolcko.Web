using System;

namespace Bolcko.Domain.Entities.Subscription
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        
        /// <summary>
        /// "Investor", "Contractor", "Vendor"
        /// </summary>
        public string TargetRole { get; set; } = string.Empty;

        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }

        /// <summary>
        /// Max financing/tender request ceiling (Contractor)
        /// </summary>
        public decimal MaxTenderLimit { get; set; } = 10000;

        /// <summary>
        /// Commission or Wakala discount %
        /// </summary>
        public decimal CommissionDiscountPercent { get; set; } = 0;

        /// <summary>
        /// Hours before public release (Investor VIP)
        /// </summary>
        public int EarlyAccessHours { get; set; } = 0;

        public bool AutoInvestAllowed { get; set; } = false;
        public string FeaturesJson { get; set; } = "[]";
        public string? BadgeText { get; set; }
        public bool IsPopular { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
