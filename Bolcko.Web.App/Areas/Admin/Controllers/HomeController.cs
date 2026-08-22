using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;

        public HomeController(IServiceManager serviceManager, UserManager<User> userManager)
        {
            _serviceManager = serviceManager;
            _userManager    = userManager;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int? companyId = null)
        {
            // ─── Tenders ─────────────────────────────────────────────────────────
            var latestTenders = await _serviceManager.TenderService.GetLatestTendersAsync(5);

            // ─── Orders ──────────────────────────────────────────────────────────
            var latestOrdersPaged = await _serviceManager.OrderService.GetPagedOrdersAsync(1, 5);
            var totalSales        = await _serviceManager.OrderService.GetTotalSalesAsync();
            var totalOrders       = await _serviceManager.OrderService.GetTotalCountAsync();

            // ─── Delivery Jobs ────────────────────────────────────────────────────
            var allJobsQuery = (await _serviceManager.DeliveryService.GetAllJobsAsync()).AsQueryable();

            if (companyId.HasValue)
                allJobsQuery = allJobsQuery.Where(j => j.DeliveryCompanyId == companyId.Value);
            if (startDate.HasValue)
                allJobsQuery = allJobsQuery.Where(j =>
                    j.AssignedAt >= startDate.Value ||
                    (j.DeliveredAt.HasValue && j.DeliveredAt >= startDate.Value));
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                allJobsQuery = allJobsQuery.Where(j =>
                    j.AssignedAt <= endOfDay ||
                    (j.DeliveredAt.HasValue && j.DeliveredAt <= endOfDay));
            }

            var jobsList = allJobsQuery.ToList();

            ViewBag.StartDate         = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate           = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCompanyId = companyId;
            ViewBag.DeliveryCompanies = await _serviceManager.DeliveryService.GetActiveCompaniesAsync();

            // ─── Financial Stats ──────────────────────────────────────────────────
            var totalDeliveryFees = jobsList
                .Where(j =>
                    j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered ||
                    j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned)
                .Sum(j => j.DeliveryFee);

            var returnedCount  = jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned);
            var totalJobsCount = jobsList.Count;
            var returnRate     = totalJobsCount > 0 ? ((decimal)returnedCount / totalJobsCount) * 100m : 0m;

            ViewBag.NetSales          = totalSales - totalDeliveryFees;
            ViewBag.TotalDeliveryFees = totalDeliveryFees;
            ViewBag.ReturnedJobsCount = returnedCount;
            ViewBag.ReturnRate        = returnRate;

            // ─── Chart: Last 7 Days ───────────────────────────────────────────────
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i)).ToList();

            ViewBag.DailyLabelsJson    = System.Text.Json.JsonSerializer.Serialize(last7Days.Select(d => d.ToString("dd/MM")));
            ViewBag.DailyDeliveredJson = System.Text.Json.JsonSerializer.Serialize(last7Days.Select(d =>
                jobsList.Count(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered)));
            ViewBag.DailyCollectedJson = System.Text.Json.JsonSerializer.Serialize(last7Days.Select(d =>
                jobsList.Where(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered)
                        .Sum(j => j.CollectedAmount ?? 0)));

            ViewBag.StatusCountsJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Available),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Assigned
                                 || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp
                                 || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.InTransit),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned)
            });

            // ─── ViewModel ────────────────────────────────────────────────────────
            var model = new Areas.Admin.Models.AdminDashboardViewModel
            {
                UserCount     = await _userManager.Users.CountAsync(),
                ProductCount  = (await _serviceManager.ProductService.GetAllProductsAsync()).Count(),
                CategoryCount = (await _serviceManager.CategoryService.GetAllCategoriesAsync()).Count(),
                TotalSales    = totalSales,
                TotalOrders   = totalOrders,
                OpenTenders   = await _serviceManager.TenderService.GetPendingCountAsync(),
                LatestTenders = latestTenders.ToList(),
                LatestOrders  = latestOrdersPaged.Items
            };

            return View(model);
        }
    }
}
