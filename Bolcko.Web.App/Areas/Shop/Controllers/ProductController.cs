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

        public async Task<IActionResult> Index(int id)
        {
            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var culture = CultureInfo.CurrentCulture.Name;
            await product.TranslateAsync(_translationService, culture, _unitOfWork);

            return View(product);
        }

        public async Task<IActionResult> Search(string query)
        {
            var products = await _serviceManager.ProductService.SearchProductsAsync(query);
            var culture = CultureInfo.CurrentCulture.Name;
            var translatedProducts = await products.TranslateAsync(_translationService, culture, _unitOfWork);

            ViewBag.Query = query;
            return View(translatedProducts);
        }
    }
}
