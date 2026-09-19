using Bolcko.Domain.Entities.Financing.DTOs;
using System.Collections.Generic;

namespace Bolcko.Web.App.Areas.Shop.Models
{
    public class ProgrammaticTendersViewModel
    {
        public string CitySlug { get; set; } = "all";
        public string CityNameAr { get; set; } = "كافة المحافظات";
        public string CityNameEn { get; set; } = "Jordan";

        public string MaterialSlug { get; set; } = "all";
        public string MaterialNameAr { get; set; } = "كافة المواد الإنشائية";
        public string MaterialNameEn { get; set; } = "All Materials";

        public string PageTitle { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;

        public IEnumerable<FinancingTenderDto> Tenders { get; set; } = new List<FinancingTenderDto>();
        public int TotalActiveOpportunities { get; set; }
        public decimal TotalSyndicateVolumeJod { get; set; }
        public decimal AverageAnnualizedYield { get; set; } = 14.8m;
        public int AverageTenureDays { get; set; } = 45;
        public decimal ShariaComplianceIndex { get; set; } = 100m;
        public double RepaymentSuccessRate { get; set; } = 99.6;

        public bool IsAuthenticatedInvestor { get; set; }

        public List<RegionalMarketMetricItem> RegionalMetrics { get; set; } = new();
        public List<CityNavOption> AvailableCities { get; set; } = new();
        public List<MaterialNavOption> AvailableMaterials { get; set; } = new();
    }

    public class RegionalMarketMetricItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Subtext { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "emerald";
    }

    public class CityNavOption
    {
        public string Slug { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int ActiveDealsCount { get; set; }
    }

    public class MaterialNavOption
    {
        public string Slug { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int ActiveDealsCount { get; set; }
    }
}
