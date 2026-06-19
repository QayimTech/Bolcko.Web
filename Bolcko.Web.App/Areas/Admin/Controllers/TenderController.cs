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

        public TenderController(BlockoDbContext dbContext)
        {
            _dbContext = dbContext;
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
    }
}
