namespace Bolcko.Domain.Entities.Catalog.DTOs
{
    public class CalculatorConfigurationDto
    {
        // ── Skeleton & Workmanship Rates (JOD) ──
        public decimal BaseResidentialLaborRate { get; set; } = 34.00m;
        public decimal BaseCommercialLaborRate { get; set; } = 38.00m;
        public decimal EightBarLaborAddition { get; set; } = 1.50m;

        // ── Stone & Facades Rates (JOD) ──
        public decimal RuwaishedStonePricePerM2 { get; set; } = 18.50m;
        public decimal MaanStonePricePerM2 { get; set; } = 24.50m;
        public decimal ArtificialStonePricePerM2 { get; set; } = 13.50m;
        public decimal TabzehFinishExtraPrice { get; set; } = 1.50m;
        public decimal MusamsamFinishExtraPrice { get; set; } = 2.00m;
        public decimal NaturalCorniceRatePerMeter { get; set; } = 12.50m;
        public decimal HybridCorniceRatePerMeter { get; set; } = 7.50m;
        public decimal NaturalWindowFrameRate { get; set; } = 38.00m;
        public decimal HybridWindowFrameRate { get; set; } = 22.00m;
        public decimal NaturalEntranceColumnRate { get; set; } = 160.00m;
        public decimal HybridEntranceColumnRate { get; set; } = 85.00m;
        public decimal StoneInstallationAndBackingRate { get; set; } = 12.00m;

        // ── Dynamic Finishing & Insulation Addons ──
        public bool EnableInsulationAddon { get; set; } = true;
        public decimal InsulationRatePerM2 { get; set; } = 4.50m; // رولات عزل زفتي للأسطح والقواعد + فوم

        public bool EnablePaintingAddon { get; set; } = true;
        public decimal InteriorPaintRatePerM2 { get; set; } = 5.50m; // معجونة وجهين + أساس ودهان سوبر ديلوكس

        public bool EnablePlasteringAddon { get; set; } = true;
        public decimal PlasteringRatePerM2 { get; set; } = 4.00m; // قصارة داخلية وخارجية (بؤج وأوتار)

        public bool EnableElectroMechanicalAddon { get; set; } = true;
        public decimal ElectroMechanicalRatePerM2 { get; set; } = 9.00m; // تأسيسات كهربائية وتمديدات صحية وتغذية

        // ── Dynamic Stone Types List ──
        public List<DynamicStoneTypeDto> DynamicStoneTypes { get; set; } = new List<DynamicStoneTypeDto>
        {
            new DynamicStoneTypeDto { Id = "Natural_Ruwaished", NameAr = "حجر رويشد طبيعي", NameEn = "Ruwaished Natural Stone", PricePerM2 = 18.50m, IsActive = true, IsDefault = true, ColorHex = "#e5d9c5", Roughness = 0.85, Metalness = 0.05 },
            new DynamicStoneTypeDto { Id = "Natural_Maan", NameAr = "حجر معان بلوري ناصع", NameEn = "Maan White Crystalline Stone", PricePerM2 = 24.50m, IsActive = true, IsDefault = false, ColorHex = "#f5f3ef", Roughness = 0.75, Metalness = 0.05 },
            new DynamicStoneTypeDto { Id = "Artificial_HighDensity", NameAr = "حجر صناعي مضغوط عالي الكثافة", NameEn = "High-Density Cast Stone", PricePerM2 = 13.50m, IsActive = true, IsDefault = false, ColorHex = "#d6cbb8", Roughness = 0.90, Metalness = 0.02 }
        };

        // ── Dynamic Decor Items List ──
        public List<DynamicDecorItemDto> DynamicDecorItems { get; set; } = new List<DynamicDecorItemDto>
        {
            new DynamicDecorItemDto { Id = "Cornice_Natural", Category = "Cornice", NameAr = "كرنيش طبيعي منحوت", NameEn = "Natural Carved Cornice", Rate = 12.50m, UnitAr = "متر طولي", UnitEn = "Linear m", IsHybrid = false },
            new DynamicDecorItemDto { Id = "Cornice_Hybrid", Category = "Cornice", NameAr = "كرنيش صناعي موفر", NameEn = "Hybrid Molded Cornice", Rate = 7.50m, UnitAr = "متر طولي", UnitEn = "Linear m", IsHybrid = true },
            new DynamicDecorItemDto { Id = "WindowFrame_Natural", Category = "WindowFrame", NameAr = "برواز شباك حجر طبيعي", NameEn = "Natural Stone Window Frame", Rate = 38.00m, UnitAr = "شباك", UnitEn = "Window", IsHybrid = false },
            new DynamicDecorItemDto { Id = "WindowFrame_Hybrid", Category = "WindowFrame", NameAr = "برواز شباك صناعي موفر", NameEn = "Hybrid Stone Window Frame", Rate = 22.00m, UnitAr = "شباك", UnitEn = "Window", IsHybrid = true },
            new DynamicDecorItemDto { Id = "EntranceColumn_Natural", Category = "Column", NameAr = "عمود مدخل حجر طبيعي وتاج", NameEn = "Natural Stone Entrance Column", Rate = 160.00m, UnitAr = "عمود", UnitEn = "Column", IsHybrid = false },
            new DynamicDecorItemDto { Id = "EntranceColumn_Hybrid", Category = "Column", NameAr = "عمود مدخل صناعي وتاج", NameEn = "Hybrid Entrance Column", Rate = 85.00m, UnitAr = "عمود", UnitEn = "Column", IsHybrid = true }
        };

        // ── Marketing & Contact Info ──
        public string SalesWhatsAppNumber { get; set; } = "962790000000";
        public string SupportPhoneNumber { get; set; } = "+962 6 000 0000";
        public string MarketingBadgeTextAr { get; set; } = "أسعار توريد مباشرة معتمدة وفق كودات البناء الأردنية 2026";
        public string MarketingBadgeTextEn { get; set; } = "Direct site supplies approved by Jordanian Building Codes 2026";
    }

    public class DynamicStoneTypeDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal PricePerM2 { get; set; } = 20.00m;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public string ColorHex { get; set; } = "#e0d5c1";
        public double Roughness { get; set; } = 0.85;
        public double Metalness { get; set; } = 0.05;
        public string? TextureUrl { get; set; }
    }

    public class DynamicDecorItemDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Category { get; set; } = "Cornice"; // Cornice, WindowFrame, Column
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal Rate { get; set; } = 10.00m;
        public string UnitAr { get; set; } = "متر طولي";
        public string UnitEn { get; set; } = "Unit";
        public bool IsHybrid { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    public class SampleBoxRequestDto
    {
        public string StoneType { get; set; } = string.Empty;
        public string StoneFinish { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? ProjectType { get; set; }
        public decimal SamplePriceJod { get; set; } = 5.0m;
    }

    public class ShowroomPassRequestDto
    {
        public string StoneType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; } = DateTime.UtcNow.AddDays(1);
        public string? EstimatedAreaM2 { get; set; }
    }
}
