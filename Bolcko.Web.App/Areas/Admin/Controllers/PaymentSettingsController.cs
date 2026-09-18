using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces.Payment;
using Bolcko.Domain.Entities.Payment;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class PaymentSettingsController : Controller
    {
        private readonly IPaymentGatewayService _paymentGatewayService;

        public PaymentSettingsController(IPaymentGatewayService paymentGatewayService)
        {
            _paymentGatewayService = paymentGatewayService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _paymentGatewayService.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PaymentGatewaySettingsDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "يرجى التحقق من صحة البيانات المدخلة.";
                return View("Index", model);
            }

            await _paymentGatewayService.SaveSettingsAsync(model);
            TempData["SuccessMessage"] = "تم حفظ وتحديث إعدادات وبوابات الدفع بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickToggle(string methodCode, bool isEnabled)
        {
            var settings = await _paymentGatewayService.GetSettingsAsync();
            switch (methodCode?.ToUpperInvariant())
            {
                case "COD":
                    settings.CodEnabled = isEnabled;
                    break;
                case "CLIQ":
                case "CLIQ_ESCROW":
                    settings.CliqEnabled = isEnabled;
                    break;
                case "EFAWATEERCOM":
                    settings.EfawateercomEnabled = isEnabled;
                    break;
                case "CARD":
                case "ONLINE_CARD":
                    settings.CardGatewayEnabled = isEnabled;
                    break;
            }

            await _paymentGatewayService.SaveSettingsAsync(settings);
            TempData["SuccessMessage"] = $"تم {(isEnabled ? "تفعيل" : "تعطيل")} وسيلة الدفع ({methodCode}) بنجاح.";
            return RedirectToAction(nameof(Index));
        }
    }
}
