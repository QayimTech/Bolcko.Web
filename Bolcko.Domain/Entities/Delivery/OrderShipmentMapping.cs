using System;

namespace Bolcko.Domain.Entities.Delivery
{
    public class OrderShipmentMapping
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProviderKey { get; set; } = "LogesTechs";
        public string? ExternalPackageId { get; set; }
        public string? Barcode { get; set; }
        public string? CodBarcode { get; set; }
        public string? AwbPdfUrl { get; set; }
        public string? CurrentStatus { get; set; }
        public string? ArabicStatus { get; set; }
        public double CodAmount { get; set; }
        public string? AssignedDriverName { get; set; }
        public string? AssignedDriverPhone { get; set; }
        public string? RawWebhookPayload { get; set; }
        public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastStatusUpdatedAt { get; set; }
    }
}
