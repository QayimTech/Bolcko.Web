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
using Microsoft.Extensions.Logging;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    // Keeping the DTO for Logestechs mapping. In a larger refactor, this could move to a separate folder.
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

        [JsonPropertyName("packageBarcode")]
        public string? PackageBarcode { get; set; }

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

        [JsonPropertyName("attachmentUrls")]
        public System.Collections.Generic.List<string>? AttachmentUrls { get; set; }
    }

    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/webhooks/delivery")]
    // Legacy support for GLC endpoints already shared
    [Route("api/v1/webhooks/logestechs")] 
    public class DeliveryWebhookController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeliveryWebhookController> _logger;

        public DeliveryWebhookController(IUnitOfWork unitOfWork, ILogger<DeliveryWebhookController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpPost("{providerKey}/{companyId?}")]
        // Using JsonElement allows the endpoint to accept ANY valid JSON without throwing 400 Bad Request Model Validation errors.
        public async Task<IActionResult> ReceiveStatusUpdate([FromRoute] string providerKey, [FromRoute] string? companyId, [FromBody] JsonElement rawPayload)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                return BadRequest(new { success = false, message = "Provider Key is missing" });
            }

            var providerLower = providerKey.ToLowerInvariant();
            _logger.LogInformation("Received Webhook from provider: {ProviderKey}, CompanyId: {CompanyId}", providerKey, companyId);

            // Generic Strategy Router
            switch (providerLower)
            {
                case "glc":
                case "logestechs":
                    return await ProcessLogestechsWebhookAsync(providerKey, companyId, rawPayload);
                
                // Future providers like Aramex, DHL, etc can be added here
                // case "aramex":
                //     return await ProcessAramexWebhookAsync(providerKey, companyId, rawPayload);

                default:
                    // If we receive a webhook for an unknown provider, we log it and return 200 so they stop retrying.
                    _logger.LogWarning("Unknown delivery provider webhook received: {ProviderKey}", providerKey);
                    return Ok(new { success = true, message = $"Webhook received but provider '{providerKey}' is not supported yet." });
            }
        }

        // =========================================================================
        // GLC / Logestechs Strategy Implementation
        // =========================================================================
        private async Task<IActionResult> ProcessLogestechsWebhookAsync(string providerKey, string? companyId, JsonElement rawPayload)
        {
            LogesTechsWebhookPayloadDto? payload = null;
            try
            {
                // Deserialize the generic JSON into the strongly-typed Logestechs DTO
                payload = JsonSerializer.Deserialize<LogesTechsWebhookPayloadDto>(rawPayload.GetRawText());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Logestechs webhook payload");
                return BadRequest(new { success = false, message = "Invalid payload format for Logestechs" });
            }

            if (payload == null)
            {
                return BadRequest(new { success = false, message = "Payload null" });
            }

            var statusStr = payload.NewStatus ?? payload.Status ?? "";
            var invoiceNum = payload.InvoiceNumber ?? "";
            var barcodeStr = payload.Barcode ?? payload.PackageBarcode ?? (payload.PackageId > 0 ? payload.PackageId.ToString() : "");

            // Log incoming Webhook using standard logger to prevent file I/O exceptions on Render
            var firstAttachment = payload.AttachmentUrls != null && payload.AttachmentUrls.Count > 0 ? payload.AttachmentUrls[0] : "";
            _logger.LogInformation("WEBHOOK RECEIVED: Provider={ProviderKey}, CompanyId={CompanyId}, Invoice={InvoiceNum}, Barcode={BarcodeStr}, Status={StatusStr}, COD={Cod}, Attachment={FirstAttachment}", 
                providerKey, companyId, invoiceNum, barcodeStr, statusStr, payload.Cod, firstAttachment);

            // Find matching OrderShipmentMapping by barcode/packageId or invoice number
            var mapping = await _unitOfWork.OrderShipmentMappings.GetAllAsQueryable()
                .FirstOrDefaultAsync(m => (barcodeStr != "" && (m.Barcode == barcodeStr || m.ExternalPackageId == barcodeStr)) || 
                                          (payload.PackageId > 0 && m.ExternalPackageId == payload.PackageId.ToString()));

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

            // Update mapping details
            if (mapping != null)
            {
                mapping.CurrentStatus = statusStr;
                mapping.ArabicStatus = MapLogesTechsStatusToArabic(statusStr);
                mapping.LastStatusUpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(payload.DriverName)) mapping.AssignedDriverName = payload.DriverName.Trim();
                if (!string.IsNullOrEmpty(payload.DriverPhone)) mapping.AssignedDriverPhone = payload.DriverPhone.Trim();
                if (!string.IsNullOrEmpty(firstAttachment)) mapping.AwbPdfUrl = firstAttachment; // Store Proof of Delivery (POD) link
                mapping.RawWebhookPayload = rawPayload.GetRawText();
            }

            // Exhaustive Dynamic Domain Order Status Mapping & Inventory Stock Restoration Engine
            if (order != null)
            {
                var upperStatus = statusStr.ToUpperInvariant();
                var previousStatus = order.Status;

                switch (upperStatus)
                {
                    // 1. Successful Delivery & Financial Settlement
                    case "DELIVERED":
                    case "DELIVERED_TO_RECIPIENT":
                    case "COMPLETED":
                    case "INVOICED":
                        order.Status = OrderStatus.Delivered;
                        order.PaymentStatus = "Paid";
                        if (payload.Cod > 0)
                        {
                            order.TotalAmount = (decimal)payload.Cod;
                        }
                        break;

                    // 2. In Transit / Driver Out for Delivery
                    case "OUT_FOR_DELIVERY":
                    case "IN_TRANSIT":
                    case "IN_TRANSIT_TO_CUSTOMER":
                    case "SCANNED_BY_DRIVER_AND_IN_CAR":
                    case "EXPORTED_TO_THIRD_PARTY":
                    case "TRANSFERRED_OUT":
                        order.Status = OrderStatus.Shipped;
                        break;

                    // 3. Order Returns / Cancellation -> Auto Stock Restored to Inventory
                    case "CANCELLED":
                    case "RETURNED_BY_RECIPIENT":
                    case "DELIVERED_TO_SENDER":
                    case "RETURNED":
                    case "REJECTED_BY_DRIVER_AND_PENDING_MANGEMENT":
                    case "DELETED":
                        order.Status = OrderStatus.Cancelled;
                        order.PaymentStatus = "Returned";

                        // Restore product quantities if not already cancelled
                        if (previousStatus != OrderStatus.Cancelled)
                        {
                            var fullOrder = await _unitOfWork.Orders.GetOrderByIdWithItemsAsync(order.Id);
                            if (fullOrder?.Items != null)
                            {
                                foreach (var item in fullOrder.Items)
                                {
                                    if (item.Product != null)
                                    {
                                        item.Product.StockQuantity += item.Quantity;
                                        _unitOfWork.Products.Update(item.Product);
                                    }
                                }
                            }
                        }
                        break;

                    // 4. Warehouse Fulfillment & Hub Processing Stages
                    case "CREATED":
                    case "DRAFT":
                    case "ACCEPTED_BY_DRIVER_AND_PENDING_PICKUP":
                    case "ASSIGNED_TO_DRIVER_AND_PENDING_APPROVAL":
                    case "IN_HUB":
                    case "RECEIVED_IN_HUB":
                    case "PICKED":
                    case "PACKED":
                    case "POSTPONED_DELIVERY":
                    case "PENDING_CUSTOMER_CARE_APPROVAL":
                    case "ARRIVED":
                    case "BROUGHT":
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
            return status.ToUpperInvariant() switch
            {
                "PENDING_CUSTOMER_CARE_APPROVAL" => "طلب جديد بانتظار الموافقة",
                "APPROVED_BY_CUSTOMER_CARE_AND_WAITING_FOR_DISPATCHER" => "جاهز للفرز والتوصيل",
                "ACCEPTED_BY_DRIVER_AND_PENDING_PICKUP" => "قبول السائق وبانتظار الاستلام",
                "SCANNED_BY_DRIVER_AND_IN_CAR" => "تم تحميل الشحنة بالمركبة 🚚",
                "OUT_FOR_DELIVERY" => "خرج للتوصيل للزبون 🛵",
                "DELIVERED_TO_RECIPIENT" => "تم التسليم للزبون بنجاح 🟢",
                "DELIVERED" => "تم التسليم للزبون بنجاح 🟢",
                "COMPLETED" => "شحنة مكتملة ومغلقة 🟢",
                "POSTPONED_DELIVERY" => "تأجيل التوصيل لموعد آخر ⏰",
                "RETURNED_BY_RECIPIENT" => "تم إرجاع الشحنة من الزبون 🔴",
                "DELIVERED_TO_SENDER" => "تم إرجاع الشحنة للمستودع 🔄",
                "CANCELLED" => "شحنة ملغاة ❌",
                "PICKED" => "تم تجهيز وتجميع المنتجات 📦",
                "PACKED" => "تم تغليف وتجهيز الشحنة 📦",
                "IN_HUB" => "في مركز التوزيع (الفرز) 🏢",
                _ => status
            };
        }
    }
}
