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
        private readonly IDeliveryApiService _deliveryApiService;

        public DeliverySettingsController(IDeliveryApiService deliveryApiService)
        {
            _deliveryApiService = deliveryApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var configs = await uow.DeliveryProviderConfigs.GetAllAsync();
            
            var scheme = Request.Host.Host.Contains("localhost") ? Request.Scheme : "https";
            var baseUrl = $"{scheme}://{Request.Host}";
            ViewBag.BaseWebhookUrl = $"{baseUrl}/api/v1/webhooks/delivery";
            
            ViewBag.ShippingRates = await uow.ShippingRates.GetAllAsync();

            var enableExpressSetting = await uow.AppSettings.GetByKeyAsync("EnableExpressDelivery");
            var feeSetting = await uow.AppSettings.GetByKeyAsync("ExpressDeliveryFee");
            ViewBag.EnableExpressDelivery = enableExpressSetting?.Value?.ToLower() == "true" ? "true" : "false";
            ViewBag.ExpressDeliveryFee = feeSetting?.Value ?? "5.00";

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
        public async Task<IActionResult> DeleteProvider(int id)
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var config = await uow.DeliveryProviderConfigs.GetByIdAsync(id);
            if (config != null)
            {
                uow.DeliveryProviderConfigs.Remove(config);
                await uow.CompleteAsync();
                TempData["SuccessMessage"] = $"تم حذف شركة التوصيل ({config.ProviderName}) وإزالة إعداداتها بنجاح 🗑️";
                return Json(new { success = true, message = "تم حذف شركة التوصيل بنجاح" });
            }
            return Json(new { success = false, message = "لم يتم العثور على الشركة المراد حذفها" });
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
            var config = await _deliveryApiService.GetActiveConfigAsync();
            if (config == null || string.IsNullOrEmpty(config.CompanyId))
            {
                return Json(new { success = false, message = "لم يتم تفعيل إعدادات شركة التوصيل بعد." });
            }

            var villages = await _deliveryApiService.GetVillagesAsync();
            if (villages != null && villages.Any())
            {
                return Json(new { success = true, message = "تم فحص الاتصال بسيرفر API التوصيل بنجاح! الإعدادات صحيحة ومفعلة ⚡" });
            }

            return Json(new { success = false, message = "فشل فحص الاتصال بسيرفر API التوصيل." });
        }

        [HttpPost]
        public async Task<IActionResult> SyncLocations(string? providerKey)
        {
            try
            {
                var count = await _deliveryApiService.SyncProviderLocationsAsync(providerKey);
                if (count > 0)
                {
                    return Json(new { success = true, message = $"✅ تم مزامنة وفهرسة {count} مدينة ومنطقة وقرية في جدول التوصيل بنجاح!" });
                }
                return Json(new { success = false, message = "تعذر الاتصال بـ API القرى للشركة أو لا توجد قرى مرجعة." });
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("logs/delivery_api.log", $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] SyncLocations Controller EXCEPTION: {ex.Message}\n{ex.StackTrace}\n================ failure ===============\n");
                return Json(new { success = false, message = $"❌ خطأ أثناء المزامنة: {ex.Message}" });
            }
        }

        // ==========================================
        // Shipping Rates (Governorates)
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> AddShippingRate(string cityName, string? cityNameEn, decimal rate)
        {
            if (string.IsNullOrWhiteSpace(cityName) || rate < 0)
            {
                TempData["ErrorMessage"] = "يرجى إدخال اسم محافظة صحيح وقيمة توصيل صالحة.";
                return RedirectToAction(nameof(Index));
            }

            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var allRates = await uow.ShippingRates.GetAllAsync();
            if (allRates.Any(r => r.CityName.Trim().ToLower() == cityName.Trim().ToLower()))
            {
                TempData["ErrorMessage"] = "هذه المحافظة موجودة مسبقاً.";
                return RedirectToAction(nameof(Index));
            }

            var newRate = new Bolcko.Domain.Entities.Setting.ShippingRate { CityName = cityName.Trim(), CityNameEn = cityNameEn?.Trim(), Rate = rate };
            await uow.ShippingRates.AddAsync(newRate);
            await uow.CompleteAsync();

            TempData["SuccessMessage"] = "تم إضافة المحافظة بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShippingRate(int id, string cityName, string? cityNameEn, decimal rate)
        {
            if (string.IsNullOrWhiteSpace(cityName) || rate < 0)
            {
                TempData["ErrorMessage"] = "البيانات المدخلة غير صحيحة.";
                return RedirectToAction(nameof(Index));
            }

            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var existingRate = await uow.ShippingRates.GetByIdAsync(id);
            if (existingRate == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المحافظة.";
                return RedirectToAction(nameof(Index));
            }

            existingRate.CityName = cityName.Trim();
            existingRate.CityNameEn = cityNameEn?.Trim();
            existingRate.Rate = rate;

            uow.ShippingRates.Update(existingRate);
            await uow.CompleteAsync();

            TempData["SuccessMessage"] = "تم تحديث سعر التوصيل للمحافظة بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteShippingRate(int id)
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var existingRate = await uow.ShippingRates.GetByIdAsync(id);
            if (existingRate == null)
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المحافظة.";
                return RedirectToAction(nameof(Index));
            }

            uow.ShippingRates.Remove(existingRate);
            await uow.CompleteAsync();

            TempData["SuccessMessage"] = "تم حذف المحافظة بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // Express Delivery Settings
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> SaveExpressDeliverySettings(bool enableExpressDelivery, decimal expressDeliveryFee)
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            await SaveSettingAsync(uow, "EnableExpressDelivery", enableExpressDelivery ? "true" : "false", "تفعيل خيار التوصيل الفوري السريع");
            await SaveSettingAsync(uow, "ExpressDeliveryFee", expressDeliveryFee.ToString("F2"), "رسوم التوصيل الفوري");
            
            TempData["SuccessMessage"] = "تم حفظ إعدادات التوصيل الفوري بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        private async Task SaveSettingAsync(Bolcko.Domain.Interfaces.IUnitOfWork uow, string key, string value, string description)
        {
            var setting = await uow.AppSettings.GetByKeyAsync(key);
            if (setting == null)
            {
                setting = new Bolcko.Domain.Entities.Setting.AppSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                };
                await uow.AppSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                setting.LastUpdated = DateTime.UtcNow;
                uow.AppSettings.Update(setting);
            }
            await uow.CompleteAsync();
        }
    }
}
