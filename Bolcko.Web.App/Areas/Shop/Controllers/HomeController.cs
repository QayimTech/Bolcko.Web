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
            var cache = (Microsoft.Extensions.Caching.Memory.IMemoryCache)HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache))!;

            // 1. Settings Cache
            var titleKey = $"HomeHeroTitle_{culture}";
            var descKey = $"HomeHeroDesc_{culture}";
            
            if (!cache.TryGetValue(titleKey, out object? titleObj) || titleObj is not string titleVal ||
                !cache.TryGetValue(descKey, out object? descObj) || descObj is not string descVal)
            {
                var titleSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "HomeHeroTitleAr" : "HomeHeroTitleEn");
                var descSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "HomeHeroDescAr" : "HomeHeroDescEn");
                titleVal = titleSetting?.Value ?? string.Empty;
                descVal = descSetting?.Value ?? string.Empty;
                
                using (var entry = cache.CreateEntry(titleKey))
                {
                    entry.Value = titleVal;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                }
                using (var entry = cache.CreateEntry(descKey))
                {
                    entry.Value = descVal;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                }
            }
            ViewBag.HomeHeroTitle = titleVal;
            ViewBag.HomeHeroDesc = descVal;

            // 2. Featured Products Cache
            var productsKey = $"Home_FeaturedProducts_{culture}";
            if (!cache.TryGetValue(productsKey, out object? productsObj) || productsObj is not IEnumerable<Bolcko.Domain.Entities.Product.DTOs.ProductDto> translatedProducts)
            {
                var featuredProducts = await _serviceManager.ProductService.GetFeaturedProductsAsync();
                translatedProducts = await featuredProducts.TranslateAsync(_translationService, culture, HttpContext.RequestServices);
                if (translatedProducts.Any())
                {
                    using (var entry = cache.CreateEntry(productsKey))
                    {
                        entry.Value = translatedProducts;
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    }
                }
            }

            // 3. Root Categories Cache
            var categoriesKey = $"Home_RootCategories_{culture}";
            if (!cache.TryGetValue(categoriesKey, out object? categoriesObj) || categoriesObj is not IEnumerable<Bolcko.Domain.Entities.Catalog.DTOs.CategoryDto> translatedCategories)
            {
                var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
                translatedCategories = await rootCategories.TranslateAsync(_translationService, culture);
                if (translatedCategories.Any())
                {
                    using (var entry = cache.CreateEntry(categoriesKey))
                    {
                        entry.Value = translatedCategories;
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    }
                }
            }
            
            var safeProducts = translatedProducts ?? Enumerable.Empty<Bolcko.Domain.Entities.Product.DTOs.ProductDto>();
            var safeCategories = translatedCategories ?? Enumerable.Empty<Bolcko.Domain.Entities.Catalog.DTOs.CategoryDto>();

            ViewData["FeaturedProducts"] = safeProducts;
            ViewData["Categories"] = safeCategories;
            ViewBag.FeaturedProducts = safeProducts;
            ViewBag.Categories = safeCategories;
            
            return View();
        }

        public async Task<IActionResult> GetMarketPrices()
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var cache = (Microsoft.Extensions.Caching.Memory.IMemoryCache)HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache))!;
            
            var cacheKey = $"Home_MarketPrices_{culture}";
            if (!cache.TryGetValue(cacheKey, out object? pricesObj) || pricesObj is not IEnumerable<Bolcko.Domain.Entities.Catalog.MarketPrice> translatedPrices)
            {
                try
                {
                    // Retrieve market prices directly from DB. Keep it lightweight and fast.
                    var prices = await _serviceManager.MarketPriceService.GetAllMarketPricesAsync();
                    if (prices != null)
                    {
                        var pricesList = prices.ToList();
                        
                        // We will skip translation APIs completely for market prices to prevent any API hangs.
                        // Instead, assign values directly based on language.
                        var isAr = culture.StartsWith("ar");
                        foreach (var p in pricesList)
                        {
                            // If Arabic, keep MaterialName as stored in DB. Otherwise, if English, use English mappings if available or direct fallback.
                            if (!isAr)
                            {
                                var name = p.MaterialName ?? "";
                                var cat = p.MaterialCategory ?? "";

                                if (cat.Equals("Steel", StringComparison.OrdinalIgnoreCase) || name.Contains("حديد"))
                                    p.MaterialName = "Steel / Rebar";
                                else if (cat.Equals("Cement", StringComparison.OrdinalIgnoreCase) || name.Contains("سمنت") || name.Contains("اسمنت") || name.Contains("إسمنت") || name.Contains("أسمنت"))
                                    p.MaterialName = "Portland Cement";
                                else if (cat.Equals("Concrete", StringComparison.OrdinalIgnoreCase) || name.Contains("خرسانة") || name.Contains("باطون"))
                                    p.MaterialName = "Ready-Mix Concrete";
                                else if (name.Contains("طوب") || name.Contains("بلك") || name.Contains("بلوك"))
                                    p.MaterialName = "Blocks / Bricks";
                                else if (name.Contains("حصمة") || name.Contains("حصى") || name.Contains("زلط"))
                                    p.MaterialName = "Gravel / Aggregate";
                                else if (name.Contains("رمل"))
                                    p.MaterialName = "Construction Sand";

                                p.UnitOfMeasure = (p.UnitOfMeasure == "طن" || p.UnitOfMeasure?.ToLower() == "ton") ? "Ton" :
                                                  (p.UnitOfMeasure == "متر مكعب" || p.UnitOfMeasure == "م3" || p.UnitOfMeasure == "م³") ? "m³" :
                                                  (p.UnitOfMeasure == "طوبة" || p.UnitOfMeasure == "حبة" || p.UnitOfMeasure == "قطعة" || p.UnitOfMeasure == "وحدة") ? "Unit" :
                                                  p.UnitOfMeasure;
                                p.Currency = "JOD";
                            }
                        }

                        translatedPrices = pricesList;
                        using (var entry = cache.CreateEntry(cacheKey))
                        {
                            entry.Value = translatedPrices;
                            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30); // Cache for 30 minutes
                        }
                    }
                    else
                    {
                        translatedPrices = new List<Bolcko.Domain.Entities.Catalog.MarketPrice>();
                    }
                }
                catch
                {
                    translatedPrices = new List<Bolcko.Domain.Entities.Catalog.MarketPrice>();
                }
            }
            
            return PartialView("Partials/_MarketPrices", translatedPrices);
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var culture = CultureInfo.CurrentCulture.Name;
            var isAr = culture.StartsWith("ar");
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var email = await uow.AppSettings.GetByKeyAsync("ContactEmail");
            var phone = await uow.AppSettings.GetByKeyAsync("ContactPhone");
            var addressSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "ContactAddress" : "ContactAddressEn");
            if (addressSetting == null || string.IsNullOrWhiteSpace(addressSetting.Value))
            {
                addressSetting = await uow.AppSettings.GetByKeyAsync("ContactAddress");
            }

            ViewBag.ContactEmail = email?.Value ?? "info@bolcko.com";
            ViewBag.ContactPhone = phone?.Value ?? "+962 6 555 5555";
            ViewBag.ContactAddress = isAr
                ? (addressSetting?.Value ?? "عمان، الأردن")
                : (!string.IsNullOrWhiteSpace(addressSetting?.Value) && !addressSetting.Value.Any(c => c >= 0x0600 && c <= 0x06FF) ? addressSetting.Value : "Amman, Jordan");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(
            string name,
            string email,
            string message,
            [FromServices] Blocko.Services.Interfaces.User.IEmailSender emailSender,
            [FromServices] Blocko.Services.Interfaces.Notifications.INotificationService notificationService,
            [FromServices] Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> localizer)
        {
            var culture = CultureInfo.CurrentCulture.Name;
            var isAr = culture.StartsWith("ar");
            var uow = (Bolcko.Domain.Interfaces.IUnitOfWork)HttpContext.RequestServices.GetService(typeof(Bolcko.Domain.Interfaces.IUnitOfWork))!;
            var contactEmailSetting = await uow.AppSettings.GetByKeyAsync("ContactEmail");
            var phoneSetting = await uow.AppSettings.GetByKeyAsync("ContactPhone");
            var addressSetting = await uow.AppSettings.GetByKeyAsync(isAr ? "ContactAddress" : "ContactAddressEn");
            if (addressSetting == null || string.IsNullOrWhiteSpace(addressSetting.Value))
            {
                addressSetting = await uow.AppSettings.GetByKeyAsync("ContactAddress");
            }

            ViewBag.ContactEmail = contactEmailSetting?.Value ?? "info@bolcko.com";
            ViewBag.ContactPhone = phoneSetting?.Value ?? "+962 6 555 5555";
            ViewBag.ContactAddress = isAr
                ? (addressSetting?.Value ?? "عمان، الأردن")
                : (!string.IsNullOrWhiteSpace(addressSetting?.Value) && !addressSetting.Value.Any(c => c >= 0x0600 && c <= 0x06FF) ? addressSetting.Value : "Amman, Jordan");

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message) || !email.Contains("@"))
            {
                ViewBag.Error = localizer["RequiredFieldsError"].Value;
                ViewBag.Name = name;
                ViewBag.Email = email;
                ViewBag.Message = message;
                return View();
            }

            try
            {
                var targetRecipient = contactEmailSetting?.Value ?? "info@bolcko.com";
                var safeName = System.Net.WebUtility.HtmlEncode(name.Trim());
                var safeEmail = System.Net.WebUtility.HtmlEncode(email.Trim());
                var safeMessage = System.Net.WebUtility.HtmlEncode(message.Trim());

                // 1. Send Admin Email Notification
                var adminSubject = $"[BLOCKO Contact Form] New message from {safeName}";
                var adminBody = Blocko.Services.Helpers.EmailTemplates.GetContactAdminTemplate(
                    safeName,
                    safeEmail,
                    safeMessage,
                    DateTime.UtcNow,
                    ViewBag.ContactPhone ?? ""
                );

                await emailSender.SendEmailAsync(targetRecipient, adminSubject, adminBody);

                // 2. Send Customer Auto-Reply Confirmation Email
                var customerSubject = isAr
                    ? "تم استلام رسالتك بنجاح | منصة بلوكو BLOCKO"
                    : "We have received your message | BLOCKO Jordan";

                var customerBody = Blocko.Services.Helpers.EmailTemplates.GetContactCustomerConfirmationTemplate(
                    safeName,
                    safeMessage,
                    ViewBag.ContactPhone ?? "+962 6 555 5555",
                    isAr
                );

                try
                {
                    await emailSender.SendEmailAsync(email.Trim(), customerSubject, customerBody);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send customer auto-reply to {Email}", email);
                }

                // 3. Send Realtime In-App Notification to Admin Dashboard
                try
                {
                    var notifTitle = isAr ? $"رسالة تواصل جديدة من {name.Trim()}" : $"New Contact Message from {name.Trim()}";
                    var previewMsg = message.Trim().Length > 100 ? message.Trim().Substring(0, 97) + "..." : message.Trim();
                    var notifBody = $"[{email.Trim()}]: {previewMsg}";
                    await notificationService.SendNotificationToRoleAsync("Admin", notifTitle, notifBody, $"/Shop/Home/Contact");
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Failed to dispatch dashboard notification for contact message from {Email}", email);
                }

                ViewBag.SuccessMessage = localizer["MessageSentSuccess"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process contact submission from {Email}", email);
                ViewBag.Error = localizer["MessageSendError"].Value;
                ViewBag.Name = name;
                ViewBag.Email = email;
                ViewBag.Message = message;
            }

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
