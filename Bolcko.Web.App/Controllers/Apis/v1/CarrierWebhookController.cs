using Blocko.Services.Interfaces.Notifications;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    public class CarrierTrackingUpdateDto
    {
        [JsonPropertyName("trackingNumber")]
        public string? TrackingNumber { get; set; }

        [JsonPropertyName("shipmentNumber")]
        public string? ShipmentNumber { get; set; }

        [JsonPropertyName("orderNumber")]
        public string? OrderNumber { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // PICKED_UP, IN_TRANSIT, ARRIVED_AT_SITE, DELIVERED, EXCEPTION

        [JsonPropertyName("carrierKey")]
        public string? CarrierKey { get; set; }

        [JsonPropertyName("driverName")]
        public string? DriverName { get; set; }

        [JsonPropertyName("driverPhone")]
        public string? DriverPhone { get; set; }

        [JsonPropertyName("vehiclePlateNumber")]
        public string? VehiclePlateNumber { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("weighbridgeNetKg")]
        public decimal? WeighbridgeNetKg { get; set; }

        [JsonPropertyName("podPhotoUrl")]
        public string? PodPhotoUrl { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/webhooks/vendor/{vendorId}/carrier-update")]
    [Route("api/v1/webhooks/carrier-update")]
    public class CarrierWebhookController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CarrierWebhookController> _logger;
        private readonly INotificationService? _notificationService;

        public CarrierWebhookController(
            IUnitOfWork unitOfWork, 
            ILogger<CarrierWebhookController> logger,
            INotificationService? notificationService = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveCarrierUpdate([FromRoute] int? vendorId, [FromBody] CarrierTrackingUpdateDto payload)
        {
            if (payload == null || (string.IsNullOrWhiteSpace(payload.TrackingNumber) && string.IsNullOrWhiteSpace(payload.ShipmentNumber) && string.IsNullOrWhiteSpace(payload.OrderNumber)))
            {
                return BadRequest(new { success = false, message = "TrackingNumber, ShipmentNumber, or OrderNumber is required." });
            }

            _logger.LogInformation("Inbound Carrier Webhook received: Vendor={VendorId}, Tracking={Tracking}, Status={Status}", 
                vendorId, payload.TrackingNumber ?? payload.ShipmentNumber, payload.Status);

            var normalizedStatus = NormalizeCarrierMilestone(payload.Status);

            // Locate order by tracking / order number
            var order = await _unitOfWork.Orders.GetAllAsQueryable(trackChanges: true)
                .FirstOrDefaultAsync(o => o.OrderNumber == payload.OrderNumber || o.OrderNumber == payload.TrackingNumber);

            if (order != null)
            {
                order.Status = normalizedStatus;
                if (normalizedStatus == OrderStatus.Delivered)
                {
                    order.PaymentStatus = "Paid";
                }
                await _unitOfWork.SaveChangesAsync();

                // Broadcast real-time status update
                if (_notificationService != null)
                {
                    try
                    {
                        var statusArabic = normalizedStatus switch
                        {
                            OrderStatus.Shipped => "خرجت الشحنة مع السائق وفي الطريق للموقع 🚚",
                            OrderStatus.Delivered => "تم تفريغ وتسليم الشحنة في الموقع بنجاح 🟢",
                            OrderStatus.Cancelled => "تم إلغاء الشحنة أو إرجاعها 🔴",
                            _ => "جاري تجهيز وتحميل الشحنة بالمستودع 📦"
                        };

                        await _notificationService.SendNotificationToRoleAsync(
                            "Contractor",
                            $"تحديث مسار الشحنة #{order.OrderNumber}",
                            $"{statusArabic} | السائق: {payload.DriverName ?? "أسطول بلكو"} ({payload.DriverPhone ?? ""})",
                            $"/Shop/Account/OrderDetails/{order.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast SignalR notification for order {OrderId}", order.Id);
                    }
                }
            }

            return Ok(new
            {
                success = true,
                message = "تم استلام ومعالجة تحديث الشحنة وتحديث الحالة اللحظية بنجاح ⚡",
                trackingNumber = payload.TrackingNumber,
                normalizedStatus = normalizedStatus.ToString(),
                timestampUtc = DateTime.UtcNow
            });
        }

        private static OrderStatus NormalizeCarrierMilestone(string rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus)) return OrderStatus.Processing;

            return rawStatus.ToUpperInvariant().Trim() switch
            {
                "PICKED_UP" or "LOADED" or "IN_HUB" or "ACCEPTED" => OrderStatus.Processing,
                "IN_TRANSIT" or "OUT_FOR_DELIVERY" or "ON_WAY" or "EN_ROUTE" => OrderStatus.Shipped,
                "ARRIVED_AT_SITE" or "UNLOADING" => OrderStatus.Shipped,
                "DELIVERED" or "COMPLETED" or "SUCCESS" => OrderStatus.Delivered,
                "CANCELLED" or "FAILED" or "REJECTED" or "RETURNED" => OrderStatus.Cancelled,
                _ => OrderStatus.Processing
            };
        }
    }
}
