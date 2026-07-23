using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Tender;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blocko.Persistence;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]

    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly BlockoDbContext _dbContext;

        public HomeController(IServiceManager serviceManager,UserManager<User> userManager, BlockoDbContext dbContext)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int? companyId = null)
        {
            var latestTenders = await _dbContext.Tenders
                .AsNoTracking()
                .Include(t => t.User)
                .OrderByDescending(t => t.RequestDate)
                .Take(5)
                .Select(t => new Bolcko.Domain.Entities.Tender.DTOs.TenderDto
                {
                    Id = t.Id,
                    TenderTitle = t.TenderTitle,
                    UserName = t.User != null ? t.User.UserName : (t.GuestName ?? "Guest"),
                    RequestDate = t.RequestDate,
                    TotalQuotedAmount = t.TotalQuotedAmount,
                    Status = t.Status
                }).ToListAsync();

            var latestOrdersPaged = await _serviceManager.OrderService.GetPagedOrdersAsync(1, 5);

            // Fetch delivery jobs for chart visualization with AsNoTracking
            var allJobsQuery = (await _serviceManager.DeliveryService.GetAllJobsAsync()).AsQueryable();

            if (companyId.HasValue)
            {
                allJobsQuery = allJobsQuery.Where(j => j.DeliveryCompanyId == companyId.Value);
            }
            if (startDate.HasValue)
            {
                allJobsQuery = allJobsQuery.Where(j => j.AssignedAt >= startDate.Value || (j.DeliveredAt.HasValue && j.DeliveredAt >= startDate.Value));
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                allJobsQuery = allJobsQuery.Where(j => j.AssignedAt <= endOfDay || (j.DeliveredAt.HasValue && j.DeliveredAt <= endOfDay));
            }

            var jobsList = allJobsQuery.ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCompanyId = companyId;
            ViewBag.DeliveryCompanies = await _serviceManager.DeliveryService.GetActiveCompaniesAsync();

            // Financial Net Sales & Return Stats Calculation for Super Admin
            var totalSales = await _dbContext.Orders.SumAsync(o => o.TotalAmount);
            var totalDeliveryFees = jobsList.Where(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned).Sum(j => j.DeliveryFee);
            var netSales = totalSales - totalDeliveryFees; // صافي المبيعات بعد خصم أجور التوصيل

            var returnedCount = jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned);
            var totalJobsCount = jobsList.Count;
            var returnRate = totalJobsCount > 0 ? ((decimal)returnedCount / totalJobsCount) * 100m : 0m;

            ViewBag.NetSales = netSales;
            ViewBag.TotalDeliveryFees = totalDeliveryFees;
            ViewBag.ReturnedJobsCount = returnedCount;
            ViewBag.ReturnRate = returnRate;

            // Prepare Chart Data for Super Admin
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .ToList();

            var dailyLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            var dailyDelivered = last7Days.Select(d => jobsList.Count(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered)).ToList();
            var dailyCollected = last7Days.Select(d => jobsList.Where(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered).Sum(j => j.CollectedAmount ?? 0)).ToList();

            ViewBag.DailyLabelsJson = System.Text.Json.JsonSerializer.Serialize(dailyLabels);
            ViewBag.DailyDeliveredJson = System.Text.Json.JsonSerializer.Serialize(dailyDelivered);
            ViewBag.DailyCollectedJson = System.Text.Json.JsonSerializer.Serialize(dailyCollected);

            var statusCounts = new[]
            {
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Available),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Assigned || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.InTransit),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered),
                jobsList.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned)
            };
            ViewBag.StatusCountsJson = System.Text.Json.JsonSerializer.Serialize(statusCounts);

            var model = new Bolcko.Web.App.Areas.Admin.Models.AdminDashboardViewModel
            {
                UserCount = await _userManager.Users.CountAsync(),
                ProductCount = (await _serviceManager.ProductService.GetAllProductsAsync()).Count(),
                CategoryCount = (await _serviceManager.CategoryService.GetAllCategoriesAsync()).Count(),
                TotalSales = await _dbContext.Orders.SumAsync(o => o.TotalAmount),
                TotalOrders = await _dbContext.Orders.CountAsync(),
                OpenTenders = await _dbContext.Tenders.CountAsync(t => t.Status == Bolcko.Domain.Enums.TenderStatus.Pending),
                LatestTenders = latestTenders,
                LatestOrders = latestOrdersPaged.Items
            };
            return View(model);
        }
    }
}
