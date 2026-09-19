using System;
using System.Collections.Generic;
using Bolcko.Domain.Entities.Subscription.DTOs;

namespace Bolcko.Web.App.Areas.Vendor.Models
{
    public class VendorSubscriptionViewModel
    {
        public int VendorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ActiveTier { get; set; } = "Standard"; // "Standard", "Silver", "Gold"
        public string ActiveTierNameAr { get; set; } = "التاجر المعتمد (الأساسي)";
        public decimal CurrentCommissionRate { get; set; } = 3.5m;
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool AutoRenew { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Exposure & Performance Analytics (SUB-01 / SUB-03)
        public int MonthlySearchImpressions { get; set; } = 24800;
        public int BuyBoxWinRatePercent { get; set; } = 84;
        public int CatalogProductsCount { get; set; } = 1229;
        public double CatalogBoostMultiplier { get; set; } = 1.5;

        // Available Vendor Subscription Plans
        public List<SubscriptionPlanDto> AvailablePlans { get; set; } = new();

        // Billing History
        public List<UserSubscriptionDto> BillingHistory { get; set; } = new();
    }

    public class UpgradePlanRequest
    {
        public int PlanId { get; set; }
        public string BillingCycle { get; set; } = "Monthly"; // "Monthly" or "Yearly"
        public string PaymentMethod { get; set; } = "CliQ";  // "CliQ", "VisaMasterCard", "Wallet"
        public string? CliqAliasOrRef { get; set; }
    }
}
