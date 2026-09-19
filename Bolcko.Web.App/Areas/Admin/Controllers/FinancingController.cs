using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,DashboardUser")]
    public class FinancingController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User>? _userManager;

        public FinancingController(IServiceManager serviceManager, UserManager<User>? userManager = null)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
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
        /// قائمة المستثمرين الماليين وتدقيق الملاءمة ووثائق الـ AML
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Investors()
        {
            if (_userManager == null)
            {
                return View(new System.Collections.Generic.List<User>());
            }

            var investors = await _userManager.Users
                .Where(u => u.UserType == UserType.Investor)
                .OrderByDescending(u => u.RegistrationDate)
                .ToListAsync();

            return View(investors);
        }

        /// <summary>
        /// اعتماد وتفعيل محفظة المستثمر بضغطة زر
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveInvestor(int id)
        {
            if (_userManager != null)
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    TempData["SuccessMessage"] = $"تم اعتماد وتفعيل محفظة المستثمر ({user.FirstName} - {user.CompanyName}) بنجاح.";
                }
            }
            return RedirectToAction(nameof(Investors));
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
