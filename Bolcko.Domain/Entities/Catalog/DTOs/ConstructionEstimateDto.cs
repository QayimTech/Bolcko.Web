using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class ConstructionEstimateRequestDto
    {
        public double BuiltUpAreaSquareMeters { get; set; } = 250;
        public int NumberOfFloors { get; set; } = 2;
        public string BuildingType { get; set; } = "Residential"; // Residential, Commercial, SkeletonOnly
        public string FoundationType { get; set; } = "IsolatedFootings"; // IsolatedFootings, Raft
        public string City { get; set; } = "Amman"; // Amman, Zarqa, Irbid, Aqaba, etc.
        public bool IncludeFinishing { get; set; } = false;

        // 1. Structural Rebar Flexibility (Engineering Specifications)
        public string ColumnBarsCount { get; set; } = "6Bars"; // "6Bars", "8Bars", "10Bars"
        public string RebarDensityGrade { get; set; } = "Standard"; // "Economic" (38kg/m2), "Standard" (42kg/m2), "Heavy" (50kg/m2), "Custom"
        public double? CustomRebarKgPerM2 { get; set; } = null;
        public string SlabSystem { get; set; } = "Ribbed"; // "Ribbed" (عقدة عصب), "Solid" (بلاطة مصمتة), "FlatSlab" (فلات سلاب)

        // 2. Stone & Facades Module
        public bool EnableStoneModule { get; set; } = true;
        public int StoneFacadesCount { get; set; } = 4; // 1, 2, 3, 4 facades
        public string StoneType { get; set; } = "Natural_Ruwaished"; // "Natural_Ruwaished", "Natural_Maan", "Artificial_HighDensity"
        public string StoneFinish { get; set; } = "Mufajjar"; // "Mufajjar" (مفجر), "Tabzeh" (طبزة), "Musamsam" (مسمسم), "Smooth" (مجلي/ناعم)
        public bool IncludeCorniceBelt { get; set; } = true; // أحزمة كرانيش بين الطوابق
        public int WindowFramesCount { get; set; } = 8; // براويز وأقواس شبابيك
        public int EntranceColumnsCount { get; set; } = 2; // أعمدة حجرية للمدخل
        public bool HybridArtificialTrim { get; set; } = false; // ديكورات حجر صناعي مع حجر طبيعي للتوفير
        public bool IncludeStoneInstallation { get; set; } = true; // توريد مع مصنعية وباطون حشوة وشناكل
    }

    public class MaterialEstimateItem
    {
        public string ItemNameAr { get; set; } = string.Empty;
        public string ItemNameEn { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPriceJod { get; set; }
        public decimal TotalPriceJod { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class ConstructionEstimateResultDto
    {
        public double TotalBuiltUpArea { get; set; }
        public int NumberOfFloors { get; set; }
        public string City { get; set; } = "Amman";
        
        // Quantities Summary (Skeleton)
        public double SteelQuantityTons { get; set; }
        public decimal SteelCostJod { get; set; }
        public double EffectiveRebarRatioKgPerM2 { get; set; }

        public double ConcreteCubicMeters { get; set; }
        public decimal ConcreteCostJod { get; set; }

        public int CementBagsCount { get; set; }
        public decimal CementCostJod { get; set; }

        public int MasonryBlocksCount { get; set; }
        public decimal MasonryBlocksCostJod { get; set; }

        public double SandAggregatesCubicMeters { get; set; }
        public decimal SandAggregatesCostJod { get; set; }

        public decimal EstimatedLaborCostJod { get; set; }
        public decimal TotalSkeletonCostJod { get; set; }
        public decimal CostPerSquareMeterJod { get; set; }

        // Stone & Facade Estimation Summary
        public bool StoneModuleEnabled { get; set; }
        public double StoneNetAreaM2 { get; set; }
        public decimal StoneMaterialCostJod { get; set; }
        public double CorniceLinearMeters { get; set; }
        public decimal CorniceCostJod { get; set; }
        public decimal WindowFramesCostJod { get; set; }
        public decimal EntranceColumnsCostJod { get; set; }
        public decimal StoneAccessoriesLaborCostJod { get; set; }
        public decimal TotalStoneCostJod { get; set; }
        public decimal PotentialSavingsJod { get; set; }

        // Grand Combined Total (Skeleton + Stone)
        public decimal GrandTotalCostJod { get; set; }

        public List<MaterialEstimateItem> MaterialItems { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
