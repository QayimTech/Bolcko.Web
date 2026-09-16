using System;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryProviderConfig
    {
        public int Id { get; set; }
        public string ProviderName { get; set; } = "LogesTechs";
        public string ProviderKey { get; set; } = "LogesTechs";
        public string BaseUrl { get; set; } = "https://apisv2.logestechs.com/api";
        public string CompanyId { get; set; } = string.Empty;
        public string ApiEmail { get; set; } = string.Empty;
        public string ApiPassword { get; set; } = string.Empty;
        public string? WebhookSecret { get; set; }
        public string? OutboundWebhookUrl { get; set; }
        public string? CustomHeadersJson { get; set; }
        public string? CustomPayloadMappingJson { get; set; }

        // Origin / Warehouse Location Settings (Ras Al-Ain)
        public string PickupAddressLine { get; set; } = "عمان - رأس العين - مستودع القناص";
        public long PickupCityId { get; set; } = 1130;
        public long PickupRegionId { get; set; } = 33;
        public long PickupVillageId { get; set; } = 6176;

        // Sender Info
        public string SenderStoreName { get; set; } = "متجر بلوكو لتوريدات البناء";
        public string SenderPhone { get; set; } = "0782023800";
        public decimal DefaultDeliveryFee { get; set; } = 3.00m;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
