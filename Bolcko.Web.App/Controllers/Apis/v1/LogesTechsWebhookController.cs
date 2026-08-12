using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Delivery;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    public class LogesTechsWebhookPayloadDto
    {
        [JsonPropertyName("packageId")]
        public long PackageId { get; set; }

        [JsonPropertyName("newStatus")]
        public string? NewStatus { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }

        [JsonPropertyName("cod")]
        public double Cod { get; set; }

        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("driverName")]
        public string? DriverName { get; set; }

        [JsonPropertyName("driverPhone")]
        public string? DriverPhone { get; set; }
    }

    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/webhooks/logestechs")]
    public class LogesTechsWebhookController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogesTechsWebhookController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [HttpPost("{companyId}")]
        public async Task<IActionResult> ReceiveStatusUpdate([FromBody] LogesTechsWebhookPayloadDto payload)
        {
            if (payload == null)
            {
                return BadRequest(new { success = false, message = "Payload null" });
            }

            var statusStr = payload.NewStatus ?? payload.Status ?? "";
            var invoiceNum = payload.InvoiceNumber ?? "";
            var barcodeStr = payload.Barcode ?? payload.PackageId.ToString();

            // Find matching OrderShipmentMapping by invoice number or barcode/packageId
            var mapping = await _unitOfWork.OrderShipmentMappings.GetAllAsQueryable()
                .FirstOrDefaultAsync(m => m.Barcode == barcodeStr || m.ExternalPackageId == payload.PackageId.ToString());

            Bolcko.Domain.Entities.Order.Order? order = null;

            if (mapping != null)
            {
                order = await _unitOfWork.Orders.GetByIdAsync(mapping.OrderId);
            }
            else if (!string.IsNullOrEmpty(invoiceNum))
            {
                order = await _unitOfWork.Orders.GetAllAsQueryable()
                    .FirstOrDefaultAsync(o => o.OrderNumber == invoiceNum);

                if (order != null)
                {
                    mapping = await _unitOfWork.OrderShipmentMappings.GetAllAsQueryable()
                        .FirstOrDefaultAsync(m => m.OrderId == order.Id);
                }
            }

            if (order == null && mapping == null)
            {
                return NotFound(new { success = false, message = "لم يتم العثور على الطلب المرتبط بهذا التحديث" });
            }

            // Update mapping
            if (mapping != null)
            {
                mapping.CurrentStatus = statusStr;
                mapping.ArabicStatus = MapLogesTechsStatusToArabic(statusStr);
                mapping.LastStatusUpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(payload.DriverName)) mapping.AssignedDriverName = payload.DriverName;
                if (!string.IsNullOrEmpty(payload.DriverPhone)) mapping.AssignedDriverPhone = payload.DriverPhone;
                mapping.RawWebhookPayload = JsonSerializer.Serialize(payload);
            }

            // Update Order Domain Status & Finance Reconciliation
            if (order != null)
            {
                switch (statusStr.ToUpper())
                {
                    case "DELIVERED_TO_RECIPIENT":
                    case "COMPLETED":
                    case "DELIVERED":
                        order.Status = OrderStatus.Delivered;
                        order.PaymentStatus = "Paid"; // COD Collected successfully
                        break;

                    case "OUT_FOR_DELIVERY":
                    case "IN_TRANSIT":
                    case "IN_TRANSIT_TO_CUSTOMER":
                    case "SCANNED_BY_DRIVER_AND_IN_CAR":
                        order.Status = OrderStatus.Shipped;
                        break;

                    case "CANCELLED":
                    case "RETURNED_BY_RECIPIENT":
                    case "DELIVERED_TO_SENDER":
                        order.Status = OrderStatus.Cancelled;
                        break;

                    case "ACCEPTED_BY_DRIVER_AND_PENDING_PICKUP":
                    case "ASSIGNED_TO_DRIVER_AND_PENDING_APPROVAL":
                        order.Status = OrderStatus.Processing;
                        break;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "تم معالجة وتحديث حالة الشحنة والطلب بنجاح ⚡",
                orderId = order?.Id,
                status = statusStr
            });
        }

        private static string MapLogesTechsStatusToArabic(string status)
        {
            return status.ToUpper() switch
            {
                "PENDING_CUSTOMER_CARE_APPROVAL" => "طلب جديد بانتظار الموافقة",
                "APPROVED_BY_CUSTOMER_CARE_AND_WAITING_FOR_DISPATCHER" => "جاهز للفرز والتوصيل",
                "ACCEPTED_BY_DRIVER_AND_PENDING_PICKUP" => "قبول السائق وبانتظار الاستلام",
                "SCANNED_BY_DRIVER_AND_IN_CAR" => "تم تحميل الشحنة بالمركبة 🚚",
                "OUT_FOR_DELIVERY" => "خرج للتوصيل للزبون 🛵",
                "DELIVERED_TO_RECIPIENT" => "تم التسليم للزبون بنجاح 🟢",
                "COMPLETED" => "شحنة مكتملة ومغلقة 🟢",
                "POSTPONED_DELIVERY" => "تأجيل التوصيل لموعد آخر ⏰",
                "RETURNED_BY_RECIPIENT" => "تم إرجاع الشحنة من الزبون 🔴",
                "CANCELLED" => "شحنة ملغاة ❌",
                _ => status
            };
        }
    }
}
