using System.Diagnostics;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;

using Blocko.Services.Interfaces;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IServiceManager serviceManager, ILogger<HomeController> logger)
        {
            _serviceManager = serviceManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _serviceManager.ProductService.GetFeaturedProductsAsync();
            var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            
            // For now, passing to ViewData or creating a ViewModel would be better
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = rootCategories;
            
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
