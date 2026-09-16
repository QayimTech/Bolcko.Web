using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class FinancingController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public FinancingController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// لوحة إدارة التمويل بالمرابحة الإسلامية والرقابة الشرعية
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var overview = await _serviceManager.FinancingService.GetAdminFinancingOverviewAsync();
            return View(overview);
        }

        /// <summary>
        /// تفاصيل العطاء وتدقيق وثائق التسليم والـ GPS
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
        }

        /// <summary>
        /// تسوية العطاء من لوحة الإدارة
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(int id)
        {
            var success = await _serviceManager.FinancingService.SettleTenderAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "تمت تسوية العطاء بنجاح وتوزيع العوائد على المستثمر واقتطاع أجر الوكالة."
                : "حدث خطأ أثناء تسوية العطاء.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
