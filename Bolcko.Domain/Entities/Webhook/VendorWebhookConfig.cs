using Bolcko.Domain.Common;
using System;

namespace Bolcko.Domain.Entities.Webhook
{
    public class VendorWebhookConfig : BaseEntity
    {
        public int VendorId { get; set; }
        public string VendorKey { get; set; } = "qannas";
        public string EndpointUrl { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        
        // JSON array or comma-separated list of subscribed topics
        public string SubscribedEvents { get; set; } = "order.placed,order.escrow_funded,order.dispatch_ready,order.cancelled,shipment.status_updated";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public int SuccessfulDeliveriesCount { get; set; }
        public int FailedDeliveriesCount { get; set; }
    }

    public class WebhookDeliveryLog : BaseEntity
    {
        public int VendorId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public string? SignatureHeader { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public int AttemptCount { get; set; } = 1;
        public bool IsSuccess { get; set; }
        public long DurationMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
