using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Catalog
{
    public class MarketPrice : BaseEntity
    {
        public string MaterialName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string Currency { get; set; } = "JD";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string? Source { get; set; }
    }
}