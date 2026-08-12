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

        public async Task<IActionResult> Index()
        {
            var config = await _logesTechsApiService.GetActiveConfigAsync() ?? new DeliveryProviderConfig
            {
                ProviderName = "LogesTechs",
                ProviderKey = "LogesTechs",
                BaseUrl = "https://apisv2.logestechs.com/api",
                IsActive = true
            };

            var request = HttpContext.Request;
            var realCompanyId = string.IsNullOrWhiteSpace(config.CompanyId) ? "YOUR_COMPANY_ID" : config.CompanyId;
            ViewBag.WebhookUrl = $"{request.Scheme}://{request.Host}/api/v1/webhooks/logestechs/{realCompanyId}";

            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(DeliveryProviderConfig config)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى التأكد من ملء كافة الحقول المطلوبة بشكل صحيح.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _logesTechsApiService.SaveConfigAsync(config);
            if (success)
            {
                TempData["SuccessMessage"] = "تم حفظ وتحديث إعدادات ربط LogesTechs API ديناميكياً بنجاح! ⚡";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء حفظ الإعدادات في قاعدة البيانات.";
            }

            return RedirectToAction(nameof(Index));
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
