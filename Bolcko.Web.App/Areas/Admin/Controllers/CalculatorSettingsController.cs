using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class CalculatorSettingsController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public CalculatorSettingsController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // GET: Admin/CalculatorSettings
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var config = await _serviceManager.MarketPriceService.GetCalculatorConfigurationAsync();
            return View(config);
        }

        // POST: Admin/CalculatorSettings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(CalculatorConfigurationDto model)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.MarketPriceService.SaveCalculatorConfigurationAsync(model);
                TempData["SuccessMessage"] = "تم تحديث إعدادات ومعاملات حاسبة البناء والإضافات بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "يرجى التحقق من صحة المدخلات وإعادة المحاولة.";
            return View(nameof(Index), model);
        }
    }
}
