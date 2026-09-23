using System;
using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.Delivery
{
    /// <summary>
    /// LOG-07: In-Trip Masked Communication & Official WhatsApp Milestone Dispatch
    /// Protects customer/driver personal phone numbers to prevent off-platform disintermediation.
    /// </summary>
    public class DeliveryTripMessage : BaseEntity
    {
        public int JobId { get; set; }
        public DeliveryJob? Job { get; set; }

        public int? OrderId { get; set; }

        public int? SenderUserId { get; set; }
        public string SenderRole { get; set; } = "Driver"; // "Driver", "Vendor", "Contractor", "Platform"
        public string SenderDisplayName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        
        // Coordination categories: "General", "GateCoordinates", "UnloadingDirections", "CraneAccess", "MilestoneWhatsApp"
        public string? Category { get; set; } = "General";
        
        public bool IsSystemNotification { get; set; } = false;
        public bool IsDeliveredViaWhatsApp { get; set; } = false;
        public string? WhatsAppTemplateName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
