using System.Threading.Tasks;
using Blocko.Services.Interfaces.Delivery;
using Bolcko.Domain.Entities.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DeliverySettingsController : Controller
    {
        private readonly ILogesTechsApiService _logesTechsApiService;

        public DeliverySettingsController(ILogesTechsApiService logesTechsApiService)
        {
            _logesTechsApiService = logesTechsApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var configs = await uow.DeliveryProviderConfigs.GetAllAsync();
            
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            ViewBag.BaseWebhookUrl = $"{baseUrl}/api/v1/webhooks/delivery";
            
            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(DeliveryProviderConfig model)
        {
            if (string.IsNullOrWhiteSpace(model.ProviderKey))
            {
                model.ProviderKey = (model.ProviderName ?? "Provider").Replace(" ", "").ToLower();
            }

            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var existing = await uow.DeliveryProviderConfigs.GetByIdAsync(model.Id);

            if (existing == null)
            {
                await uow.DeliveryProviderConfigs.AddAsync(model);
                TempData["SuccessMessage"] = $"تم إضافة وإعداد شركة التوصيل ({model.ProviderName}) بنجاح! 🚀";
            }
            else
            {
                existing.ProviderName = model.ProviderName;
                existing.ProviderKey = model.ProviderKey;
                existing.BaseUrl = model.BaseUrl;
                existing.CompanyId = model.CompanyId;
                existing.ApiEmail = model.ApiEmail;
                existing.ApiPassword = model.ApiPassword;
                existing.OutboundWebhookUrl = model.OutboundWebhookUrl;
                existing.CustomHeadersJson = model.CustomHeadersJson;
                existing.CustomPayloadMappingJson = model.CustomPayloadMappingJson;
                existing.IsActive = model.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                uow.DeliveryProviderConfigs.Update(existing);
                TempData["SuccessMessage"] = $"تم تحديث إعدادات شركة التوصيل ({model.ProviderName}) بنجاح! 💾";
            }

            await uow.CompleteAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var config = await uow.DeliveryProviderConfigs.GetByIdAsync(id);
            if (config != null)
            {
                config.IsActive = !config.IsActive;
                uow.DeliveryProviderConfigs.Update(config);
                await uow.CompleteAsync();
                return Json(new { success = true, isActive = config.IsActive, message = "تم تغيير حالة تفعيل الشركة بنجاح" });
            }
            return Json(new { success = false, message = "لم يتم العثور على الشركة" });
        }

        [HttpPost]
        public async Task<IActionResult> TestProviderConnection([FromBody] DeliveryProviderConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.BaseUrl) || string.IsNullOrWhiteSpace(config.ApiEmail))
            {
                return Json(new { success = false, message = "يرجى ملء عنوان الـ API والبريد الإلكتروني لاختبار الاتصال." });
            }

            try
            {
                var client = new System.Net.Http.HttpClient();
                var requestUrl = config.BaseUrl.Contains("/ship/request") 
                    ? config.BaseUrl.Trim() 
                    : $"{config.BaseUrl.TrimEnd('/')}/ship/request/by-email";

                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, requestUrl);
                
                if (!string.IsNullOrWhiteSpace(config.CompanyId))
                {
                    request.Headers.Add("company-id", config.CompanyId);
                }

                var testPayload = new
                {
                    email = config.ApiEmail,
                    password = config.ApiPassword,
                    pkgUnitType = "METRIC",
                    pkg = new
                    {
                        cod = 1.0,
                        invoiceNumber = $"TEST-{DateTime.UtcNow.Ticks.ToString().Substring(10)}",
                        receiverName = "اختبار اتصال بلوكو",
                        receiverPhone = "0590000000",
                        serviceType = "STANDARD",
                        shipmentType = "COD",
                        paymentType = "CASH",
                        quantity = 1,
                        contents = "شحنة تجريبية لفحص الـ API",
                        notes = "اختبار صحة مفاتيح الاعتماد بالـ API"
                    },
                    destinationAddress = new { addressLine1 = "رام الله - الماصيون", cityId = 1, regionId = 1, villageId = 1 },
                    originAddress = new { cityId = 1, regionId = 1, villageId = 1 }
                };

                request.Content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(testPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.SendAsync(request);
                var responseStr = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "🚀 تم فحص واختبار الـ API بنجاح 100%! سستم شركة التوصيل استلم الطلب التجريبي واسترجع استجابة ناجحة!" });
                }

                return Json(new { success = false, message = $"❌ فشل اختبار الاتصال بالـ API ({response.StatusCode}): {responseStr}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ خطأ في الاتصال بالشبكة أو الـ URL: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection()
        {
            var config = await _logesTechsApiService.GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId))
            {
                return Json(new { success = false, message = "لم يتم تحديد إعدادات شركة التوصيل LogesTechs بعد." });
            }

            var villages = await _logesTechsApiService.GetVillagesAsync();
            if (villages != null && villages.Count > 0)
            {
                return Json(new { success = true, message = $"تم الاتصال بنجاح بـ LogesTechs API! تم استرجاع {villages.Count} منطقة توصيل 🟢" });
            }

            return Json(new { success = false, message = "تعذّر جلب البيانات من LogesTechs API. يرجى مراجعة الـ Company-Id والإيميل والباسورد." });
        }
    }
}
