using System.Diagnostics;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var isAr = culture.StartsWith("ar");
            
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            
            var titleSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "HomeHeroTitleAr" : "HomeHeroTitleEn");
            var descSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "HomeHeroDescAr" : "HomeHeroDescEn");

            ViewBag.HomeHeroTitle = titleSetting?.Value;
            ViewBag.HomeHeroDesc = descSetting?.Value;

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
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var prices = await _serviceManager.MarketPriceService.GetAllMarketPricesAsync();
            var translatedPrices = await prices.TranslateAsync(_translationService, culture);
            return PartialView("Partials/_MarketPrices", translatedPrices);
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var email = await uow.AppSettings.GetByKeyAsync("ContactEmail");
            var phone = await uow.AppSettings.GetByKeyAsync("ContactPhone");
            var address = await uow.AppSettings.GetByKeyAsync("ContactAddress");

            ViewBag.ContactEmail = email?.Value ?? "info@bolcko.com";
            ViewBag.ContactPhone = phone?.Value ?? "+962 6 555 5555";
            ViewBag.ContactAddress = address?.Value ?? "عمان، الأردن";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TrackOrder()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackOrder(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                ViewBag.Error = "الرجاء إدخال رقم الطلب!";
                return View();
            }

            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            // Search order either by OrderNumber or Id directly in the database to prevent loading all orders in memory
            var trimmedNum = orderNumber.Trim();
            var parsedId = 0;
            var isNumeric = int.TryParse(trimmedNum, out parsedId);
            
            // Try to extract ID from standard ORD-XXXX format
            if (trimmedNum.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase) && trimmedNum.Length > 4)
            {
                int.TryParse(trimmedNum.Substring(4), out parsedId);
                isNumeric = true;
            }

            var order = await uow.Orders.GetAllAsQueryable()
                .FirstOrDefaultAsync(o => 
                    o.OrderNumber == trimmedNum || 
                    (isNumeric && o.Id == parsedId)
                );

            if (order == null)
            {
                ViewBag.Error = "الطلب غير موجود، الرجاء التحقق من الرقم المدخل.";
                return View();
            }

            // Map order items to include product titles
            var orderDto = await _serviceManager.OrderService.GetOrderByIdAsync(order.Id);
            return View(orderDto);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
