using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.Tender.DTOs;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces;
using Bolcko.Web.App.Extensions;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class QuoteController : Controller
    {
        private readonly ITenderService _tenderService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> _userManager;
        private readonly IServiceManager _serviceManager;
        private readonly ITranslationService _translationService;

        public QuoteController(
            ITenderService tenderService, 
            Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> userManager, 
            IServiceManager serviceManager,
            ITranslationService translationService)
        {
            _tenderService = tenderService;
            _userManager = userManager;
            _serviceManager = serviceManager;
            _translationService = translationService;
        }

        [HttpGet]
        [Route("Shop/Quote/Request")]
        [Route("Shop/Quote/RequestQuote")]
        [Route("Quote/Request")]
        [ActionName("Request")]
        public async Task<IActionResult> RequestGet([FromQuery] QuoteRequestDto? dto)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account", new { area = "Shop", returnUrl = Url.Action("Request", "Quote", new { area = "Shop" }) });
            }

            // Clear model state to prevent validation messages on initial load
            ModelState.Clear();

            dto ??= new QuoteRequestDto();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    dto.FullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(dto.Email))
                    dto.Email = user.Email ?? "";
                if (string.IsNullOrWhiteSpace(dto.Phone))
                    dto.Phone = user.PhoneNumber ?? "";
                if (string.IsNullOrWhiteSpace(dto.CompanyName) && !string.IsNullOrWhiteSpace(user.CompanyName))
                    dto.CompanyName = user.CompanyName;
            }

            var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            var targetCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var translatedCategories = await categories.TranslateAsync(_translationService, targetCulture);

            ViewBag.CategoriesJson = System.Text.Json.JsonSerializer.Serialize(translatedCategories.Select(c => new {
                id = c.Id,
                name = c.Name
            }));

            return View("Request", dto);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCatalog(string? query, int? categoryId, int page = 1)
        {
            var pagedProducts = await _serviceManager.ProductService.SearchCatalogProductsPagedAsync(query, categoryId, page, 4);
            var targetCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var translatedProducts = await pagedProducts.TranslateAsync(_translationService, targetCulture);
            
            // Return PartialView with the list of items
            return PartialView("Partials/_CatalogProductGrid", translatedProducts.Items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(QuoteRequestDto dto)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "يرجى تسجيل الدخول أولاً للمتابعة.", redirectUrl = Url.Action("Login", "Account", new { area = "Shop", returnUrl = Url.Action("Request", "Quote", new { area = "Shop" }) }) });
                }
                return RedirectToAction("Login", "Account", new { area = "Shop", returnUrl = Url.Action("Request", "Quote", new { area = "Shop" }) });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Shop" });
            }

            if (!ModelState.IsValid)
            {
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "يرجى التحقق من المدخلات.", errors });
                }

                var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
                var targetCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
                var translatedCategories = await categories.TranslateAsync(_translationService, targetCulture);
                ViewBag.CategoriesJson = System.Text.Json.JsonSerializer.Serialize(translatedCategories.Select(c => new {
                    id = c.Id,
                    name = c.Name
                }));

                return View(dto);
            }

            var result = await _tenderService.CreateQuoteRequestAsync(dto, user.Id);

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = Url.Action(nameof(Confirmation), new { id = result.Id }) });
            }

            return RedirectToAction(nameof(Confirmation), new { id = result.Id });
        }

        public IActionResult Confirmation(int? id)
        {
            ViewBag.TenderId = id;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var tender = await _tenderService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.User = user;
            }

            return View(tender);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var success = await _tenderService.AcceptQuotationAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "تم قبول عرض السعر بنجاح وجاري تحويله لطلب رسمي.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء قبول عرض السعر.";
            }
            return RedirectToAction("Quotes", "Account", new { area = "Shop" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Negotiate(int id, Dictionary<int, decimal> targetPrices, string feedback)
        {
            var success = await _tenderService.RequestPriceNegotiationAsync(id, targetPrices, feedback);
            if (success)
            {
                TempData["SuccessMessage"] = "تم تقديم طلب التفاوض بنجاح. سيقوم فريقنا بمراجعة أسعارك المقترحة والرد عليك.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء تقديم طلب التفاوض.";
            }
            return RedirectToAction("Quotes", "Account", new { area = "Shop" });
        }
    }
}