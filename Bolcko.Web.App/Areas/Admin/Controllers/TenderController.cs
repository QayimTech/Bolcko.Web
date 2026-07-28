using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces.Tender;
using Bolcko.Domain.Entities.Tender.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class TenderController : Controller
    {
        private readonly ITenderService _tenderService;

        public TenderController(ITenderService tenderService)
            => _tenderService = tenderService;

        public async Task<IActionResult> Index()
        {
            var tenders = await _tenderService.GetAllTendersAsync();
            return View(tenders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = await _tenderService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPricing(int id, Dictionary<int, decimal> itemPrices, string? notes)
        {
            var success = await _tenderService.SubmitQuotationPricesAsync(id, itemPrices, notes);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "تم حفظ وإرسال الأسعار بنجاح إلى العميل."
                : "حدث خطأ أثناء حفظ الأسعار.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var success = await _tenderService.RejectTenderAsync(id, reason);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "تم رفض العطاء بنجاح."
                : "حدث خطأ أثناء معالجة الطلب.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
