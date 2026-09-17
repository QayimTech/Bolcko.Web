using Bolcko.Domain.Common;
using System;

namespace Bolcko.Domain.Entities.Catalog
{
    public class MaterialType : BaseEntity
    {
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        
        // Steel, Stone, ReadyMix, Cement, Blocks, Insulation, Finishing, Custom
        public string CategoryType { get; set; } = "Custom"; 
        
        // Ton, m2, m3, Bag, 1000s, Piece, lm
        public string UnitOfMeasure { get; set; } = "Unit"; 
        public decimal DefaultPriceEstimated { get; set; }
        
        // Dynamic JSON Schema for custom specifications (e.g. diameters, grades, finishes, strengths)
        public string SpecSchemaJson { get; set; } = "{}"; 
        
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
