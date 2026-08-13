using System;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryProviderLocationMapping
    {
        public int Id { get; set; }
        public string ProviderKey { get; set; } = string.Empty; // e.g. "glc", "logestechs", "aramex"
        public string SearchName { get; set; } = string.Empty; // Normalized Arabic or English city/village name e.g. "الزرقاء", "عمان", "إربد"
        public string? NormalizedSearchName { get; set; } // Stripped of "مدينة", "منطقة", "محافظة"
        public long ExternalCityId { get; set; }
        public string? ExternalCityName { get; set; }
        public long ExternalRegionId { get; set; }
        public string? ExternalRegionName { get; set; }
        public long ExternalVillageId { get; set; }
        public string? ExternalVillageName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
