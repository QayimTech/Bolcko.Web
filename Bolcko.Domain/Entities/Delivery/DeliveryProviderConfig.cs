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
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
