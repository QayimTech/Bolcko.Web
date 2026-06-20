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

        public async Task<IActionResult> Index()
        {
            var latestTenders = await _dbContext.Tenders
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
