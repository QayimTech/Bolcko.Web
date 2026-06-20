using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blocko.Persistence;
using Bolcko.Domain.Entities.Tender.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class TenderController : Controller
    {
        private readonly BlockoDbContext _dbContext;
        private readonly Blocko.Services.Interfaces.Tender.ITenderService _tenderService;

        public TenderController(BlockoDbContext dbContext, Blocko.Services.Interfaces.Tender.ITenderService tenderService)
        {
            _dbContext = dbContext;
            _tenderService = tenderService;
        }

        public async Task<IActionResult> Index()
        {
            var tenders = await _dbContext.Tenders
                .Include(t => t.User)
                .OrderByDescending(t => t.RequestDate)
                .Select(t => new TenderDto
                {
                    Id = t.Id,
                    TenderTitle = t.TenderTitle,
                    UserName = t.User != null ? t.User.UserName : (t.GuestName ?? "Guest"),
                    RequestDate = t.RequestDate,
                    TotalQuotedAmount = t.TotalQuotedAmount,
                    Status = t.Status
                }).ToListAsync();

            return View(tenders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = await _dbContext.Tenders
                .Include(t => t.User)
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tender == null) return NotFound();

            return View(tender);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPricing(int id, Dictionary<int, decimal> itemPrices, string? notes)
        {
            var success = await _tenderService.SubmitQuotationPricesAsync(id, itemPrices, notes);
            if (success)
            {
                TempData["SuccessMessage"] = "تم حفظ وإرسال الأسعار بنجاح إلى العميل.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء حفظ الأسعار.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var success = await _tenderService.RejectTenderAsync(id, reason);
            if (success)
            {
                TempData["SuccessMessage"] = "تم رفض العطاء بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء معالجة الطلب.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
