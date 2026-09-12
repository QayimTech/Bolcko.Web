using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Catalog
{
    public class CalculatorDecorItem : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = "Cornice"; // "Cornice", "WindowFrame", "Column"
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal DefaultRate { get; set; }
        public string UnitAr { get; set; } = "متر طولي";
        public string UnitEn { get; set; } = "Linear m";
        public bool IsHybrid { get; set; } = false; // true = صناعي موفر, false = طبيعي
        public string CountryCode { get; set; } = "JO";
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
