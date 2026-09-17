using System;
using System.Collections.Generic;
using Bolcko.Domain.Entities.Subscription.Enums;

namespace Bolcko.Domain.Entities.Subscription.DTOs
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public decimal MaxTenderLimit { get; set; }
        public decimal CommissionDiscountPercent { get; set; }
        public int EarlyAccessHours { get; set; }
        public bool AutoInvestAllowed { get; set; }
        public List<string> Features { get; set; } = new();
        public string? BadgeText { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class SubscribeRequestDto
    {
        public int PlanId { get; set; }
        public string BillingCycle { get; set; } = "Monthly"; // "Monthly" or "Yearly"
        public string? PaymentMethod { get; set; } = "Wallet"; // "Wallet", "CreditCard", "CliQ"
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class UserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public string PlanNameAr { get; set; } = string.Empty;
        public string PlanNameEn { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public bool IsActive => Status == SubscriptionStatus.Active && EndDate >= DateTime.UtcNow;
        public int RemainingDays => (EndDate - DateTime.UtcNow).Days > 0 ? (EndDate - DateTime.UtcNow).Days : 0;
        public decimal MaxTenderLimit { get; set; }
        public decimal CommissionDiscountPercent { get; set; }
        public int EarlyAccessHours { get; set; }
        public bool AutoInvestAllowed { get; set; }
    }

    public class SubscriptionStatsDto
    {
        public int TotalActiveSubscribers { get; set; }
        public int InvestorSubscribers { get; set; }
        public int ContractorSubscribers { get; set; }
        public int VendorSubscribers { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal TotalRevenueToDate { get; set; }
    }
}
