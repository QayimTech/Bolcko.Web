using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Tender;
using System.Security.Claims;

namespace Bolcko.Web.App.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "DashboardUser,Admin")]
    public class TenderController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public TenderController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user ID (assuming it's stored in NameIdentifier claim)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var tenders = await _serviceManager.TenderService.GetTendersByUserAsync(userId);
                return View(tenders);
            }
            
            return View(new List<Tender>());
        }

        public async Task<IActionResult> Browse()
        {
            var openTenders = await _serviceManager.TenderService.GetOpenTendersAsync();
            return View(openTenders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tender = await _serviceManager.TenderService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
        }
    }
}
