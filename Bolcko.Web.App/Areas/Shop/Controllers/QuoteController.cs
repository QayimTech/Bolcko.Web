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
        [ActionName("Request")]
        public async Task<IActionResult> RequestGet([FromQuery] QuoteRequestDto? dto)
        {
            // Clear model state to prevent validation messages on initial load
            ModelState.Clear();

            var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            var targetCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var translatedCategories = await categories.TranslateAsync(_translationService, targetCulture);

            ViewBag.CategoriesJson = System.Text.Json.JsonSerializer.Serialize(translatedCategories.Select(c => new {
                id = c.Id,
                name = c.Name
            }));

            return View("Request", dto ?? new QuoteRequestDto());
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
            if (!ModelState.IsValid)
            {
                if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "يرجى التحقق من المدخلات.", errors });
                }
                return View(dto);
            }

            int? userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "") : null;
            var result = await _tenderService.CreateQuoteRequestAsync(dto, userId);

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