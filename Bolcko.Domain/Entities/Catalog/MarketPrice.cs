using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Catalog
{
    public class MarketPrice : BaseEntity
    {
        public string MaterialName { get; set; } = string.Empty;
        public string? MaterialNameEn { get; set; }
        public string MaterialCategory { get; set; } = "General"; // Steel, Cement, Concrete, Blocks, Aggregates
        public decimal Price { get; set; }
        public decimal? PreviousPrice { get; set; }
        public decimal? GlobalPriceUsd { get; set; }
        public double ChangePercent { get; set; }
        public string Trend { get; set; } = "stable"; // up, down, stable
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string Currency { get; set; } = "JD";
        public string? Specification { get; set; }
        public bool IsLiveAutoUpdated { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string? Source { get; set; }
        public string? CityRatesJson { get; set; }
    }
}