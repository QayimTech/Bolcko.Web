using System;

namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class MarketPriceDto
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string? MaterialNameEn { get; set; }
        public string MaterialCategory { get; set; } = "General";
        public decimal Price { get; set; }
        public decimal? PreviousPrice { get; set; }
        public decimal? GlobalPriceUsd { get; set; }
        public double ChangePercent { get; set; }
        public string Trend { get; set; } = "stable";
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string Currency { get; set; } = "JD";
        public string? Specification { get; set; }
        public string FormattedPrice => $"{Price:N2} {Currency}/{UnitOfMeasure}";
        public string FormattedChange => $"{(ChangePercent >= 0 ? "+" : "")}{ChangePercent:N2}%";
        public DateTime LastUpdated { get; set; }
        public string? Source { get; set; }
    }

    public class LiveTickerDto
    {
        public decimal GlobalSteelBilletUsd { get; set; }
        public double SteelChangePercent { get; set; }
        public decimal JordanEstimatedRebarPriceJod { get; set; }
        public decimal JordanCementPriceJod { get; set; }
        public decimal JordanReadyMixConcretePriceJod { get; set; }
        public DateTime LastSyncTime { get; set; }
        public string MarketStatus { get; set; } = "Live (مباشر)";
    }
}
