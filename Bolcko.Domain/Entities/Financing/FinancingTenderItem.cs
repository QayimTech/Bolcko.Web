using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Financing
{
    public class FinancingTenderItem : BaseEntity
    {
        public int FinancingTenderId { get; set; }
        public FinancingTender? FinancingTender { get; set; }

        public string MaterialCategory { get; set; } = string.Empty; // Steel, Cement, Concrete, Stone, Blocks
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty; // Tons, m3, Bags, m2
        public decimal UnitPriceJod { get; set; }
        public decimal SubtotalJod { get; set; }
    }
}