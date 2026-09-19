using Blocko.Services.Interfaces.Webhook;
using Bolcko.Web.App.Areas.Vendor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Route("Vendor/[controller]")]
    [Authorize(Roles = "Vendor,Supplier,SuperAdmin,Admin")]
    public class WebhooksController : Controller
    {
        private readonly IOutboundWebhookService _webhookService;

        public WebhooksController(IOutboundWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int vendorId = GetVendorId();
            var config = await _webhookService.GetConfigByVendorIdAsync(vendorId);
            if (config == null)
            {
                config = await _webhookService.SaveConfigAsync(
                    vendorId, 
                    "https://erp.yourcompany-jo.com/webhooks/blocko", 
                    _webhookService.GenerateSecretKey(), 
                    true, 
                    new[] { "order.placed", "order.escrow_funded", "order.dispatch_ready", "order.cancelled" });
            }

            var logs = await _webhookService.GetDeliveryLogsAsync(vendorId);

            var host = Request.Host.Value;
            var scheme = Request.Scheme;

            var model = new VendorWebhooksViewModel
            {
                Config = config,
                DeliveryLogs = logs,
                InboundWebhookEndpoint = $"{scheme}://{host}/api/v1/webhooks/vendor/{vendorId}/carrier-update",
                SampleInboundPayload = "{\n  \"trackingNumber\": \"TRK-260920-881\",\n  \"status\": \"IN_TRANSIT\",\n  \"driverName\": \"محمد الخوالدة\",\n  \"driverPhone\": \"0791234567\",\n  \"latitude\": 31.9539,\n  \"longitude\": 35.9106\n}"
            };

            return View(model);
        }

        [HttpPost("SaveConfig")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(string endpointUrl, string secretKey, bool isActive, string[]? selectedEvents)
        {
            int vendorId = GetVendorId();
            var events = selectedEvents ?? new[] { "order.placed", "order.escrow_funded" };

            await _webhookService.SaveConfigAsync(vendorId, endpointUrl, secretKey, isActive, events);

            TempData["SuccessMessage"] = "تم حفظ إعدادات الويب هوك (ERP Webhook) وتحديث مفتاح التوقيع بنجاح ⚡";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("GenerateNewSecret")]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateNewSecret()
        {
            var newSecret = _webhookService.GenerateSecretKey();
            return Json(new { success = true, secretKey = newSecret });
        }

        [HttpPost("TestPing")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestPing()
        {
            int vendorId = GetVendorId();
            var testPayload = new
            {
                eventType = "ping.test",
                message = "Block-O Enterprise Webhook Handshake Test",
                vendorId = vendorId,
                timestamp = System.DateTime.UtcNow
            };

            var success = await _webhookService.PublishEventAsync(vendorId, "order.placed", testPayload);

            return Json(new
            {
                success = success,
                message = success 
                    ? "تم إرسال حدث الاختبار بنجاح واستلام كود 200 OK من سيرفر الـ ERP الخاص بكم!" 
                    : "تعذر إرسال حدث الاختبار. يرجى التأكد من أن الرابط متاح ويعيد كود نجاح 2xx."
            });
        }

        private int GetVendorId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) return id;
            }
            return 1;
        }
    }
}
