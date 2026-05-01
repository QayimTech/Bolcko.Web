using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "DashboardUser,Admin")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public HomeController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get stats for the dashboard user
            var productCount = (await _serviceManager.ProductService.GetFeaturedProductsAsync()).Count(); // Simplified for now
            var categoryCount = (await _serviceManager.CategoryService.GetRootCategoriesAsync()).Count();
            
            ViewBag.ProductCount = productCount;
            ViewBag.CategoryCount = categoryCount;
            
            return View();
        }
    }
}
