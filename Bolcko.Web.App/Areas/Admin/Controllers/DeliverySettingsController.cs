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
