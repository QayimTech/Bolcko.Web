using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Delivery.DTOs
{
    public class MaskedContactInfoDto
    {
        public int JobId { get; set; }
        public int OrderId { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string MaskedCustomerPhone { get; set; } = "079****000";
        public string? DriverName { get; set; }
        public string? MaskedDriverPhone { get; set; }
        public string? VendorName { get; set; }
        public string? MaterialType { get; set; }
        public decimal? WeightTons { get; set; }
        public string? DropoffLocation { get; set; }
        public string? PickupLocation { get; set; }
        public string Status { get; set; } = string.Empty;
        public string InAppCallingToken { get; set; } = string.Empty;
    }

    public class DeliveryTripMessageDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int? OrderId { get; set; }
        public string SenderRole { get; set; } = "Driver";
        public string SenderDisplayName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsSystemNotification { get; set; }
        public bool IsDeliveredViaWhatsApp { get; set; }
        public string? WhatsAppTemplateName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FormattedTime => CreatedAt.ToString("HH:mm");
        public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd");
    }

    public class SendTripMessageRequest
    {
        public int JobId { get; set; }
        public string? SenderRole { get; set; } = "Driver";
        public string? SenderDisplayName { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string? Category { get; set; } = "General";
    }

    public class SendOfficialWhatsAppNotificationRequest
    {
        public int JobId { get; set; }
        public string Milestone { get; set; } = "Dispatched"; // Dispatched, Weighed, InTransit, Arrived, Delivered
        public string RecipientRole { get; set; } = "Contractor"; // Contractor, Driver, All
        public string? CustomNote { get; set; }
    }

    public class WhatsAppNotificationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TrackingCode { get; set; } = string.Empty;
        public string RecipientRole { get; set; } = string.Empty;
        public string MaskedRecipientPhone { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string SentMessagePreview { get; set; } = string.Empty;
        public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;
    }
}
