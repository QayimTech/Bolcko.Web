using System.Diagnostics;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Web.App.Extensions;
using System.Globalization;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly ITranslationService _translationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IServiceManager serviceManager, ITranslationService translationService, ILogger<HomeController> logger)
        {
            _serviceManager = serviceManager;
            _translationService = translationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var culture = CultureInfo.CurrentCulture.Name;
            
            var featuredProducts = await _serviceManager.ProductService.GetFeaturedProductsAsync();
            var translatedProducts = await featuredProducts.TranslateAsync(_translationService, culture);

            var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            var translatedCategories = await rootCategories.TranslateAsync(_translationService, culture);
            
            ViewBag.FeaturedProducts = translatedProducts;
            ViewBag.Categories = translatedCategories;
            
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

        public IActionResult Contact()
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
