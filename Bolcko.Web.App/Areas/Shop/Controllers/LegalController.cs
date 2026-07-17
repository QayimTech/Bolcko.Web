using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class LegalController : Controller
    {
        private readonly Bolcko.Domain.Interfaces.IUnitOfWork _uow;

        public LegalController(Bolcko.Domain.Interfaces.IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IActionResult> Privacy()
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var isAr = culture.StartsWith("ar");
            var contentSetting = await _uow.AppSettings.GetByKeyAsync(isAr ? "LegalPrivacyContentAr" : "LegalPrivacyContentEn");
            ViewBag.Content = contentSetting?.Value;
            return View();
        }

        public async Task<IActionResult> Terms()
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var isAr = culture.StartsWith("ar");
            var contentSetting = await _uow.AppSettings.GetByKeyAsync(isAr ? "LegalTermsContentAr" : "LegalTermsContentEn");
            ViewBag.Content = contentSetting?.Value;
            return View();
        }

        public async Task<IActionResult> Support()
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            var isAr = culture.StartsWith("ar");
            var contentSetting = await _uow.AppSettings.GetByKeyAsync(isAr ? "LegalSupportContentAr" : "LegalSupportContentEn");
            ViewBag.Content = contentSetting?.Value;
            return View();
        }
    }
}
