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
        
        // Quantities Summary
        public double SteelQuantityTons { get; set; }
        public decimal SteelCostJod { get; set; }

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

        public List<MaterialEstimateItem> MaterialItems { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
