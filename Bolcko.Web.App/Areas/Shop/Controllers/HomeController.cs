using System.Diagnostics;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;

using Blocko.Services.Interfaces;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.Catalog.DTOs;

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
            
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = rootCategories;
            
            return View();
        }

        public async Task<IActionResult> GetMarketPrices()
        {
            var prices = await _serviceManager.MarketPriceService.GetAllMarketPricesAsync();
            return PartialView("Partials/_MarketPrices", prices);
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
