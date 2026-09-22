using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Web.App.Extensions;
using System.Globalization;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly ITranslationService _translationService;
        private readonly Bolcko.Domain.Interfaces.IUnitOfWork _unitOfWork;

        public ProductController(IServiceManager serviceManager, ITranslationService translationService, Bolcko.Domain.Interfaces.IUnitOfWork unitOfWork)
        {
            _serviceManager = serviceManager;
            _translationService = translationService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("Shop/Product/Details/{id:int}")]
        [Route("Product/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            return await Index(id);
        }

        public async Task<IActionResult> Index(int id)
        {
            if (id <= 0)
            {
                return RedirectToActionPermanent("Index", "Category", new { area = "Shop" });
            }

            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null)
            {
                // Graceful 301 Permanent Redirect to consolidate search engine link equity and eliminate 404 errors
                return RedirectToActionPermanent("Index", "Category", new { area = "Shop" });
            }

            var culture = CultureInfo.CurrentCulture.Name;
            await product.TranslateAsync(_translationService, culture, _unitOfWork);

            return View(product);
        }

        public async Task<IActionResult> Search(string query, int page = 1, int pageSize = 24)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 48);

            var products = (await _serviceManager.ProductService.SearchProductsAsync(query)).ToList();
            var totalCount = products.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var culture = CultureInfo.CurrentCulture.Name;
            var translatedProducts = await pagedProducts.TranslateAsync(_translationService, culture, _unitOfWork);

            ViewBag.Query = query;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(translatedProducts);
        }
    }
}
