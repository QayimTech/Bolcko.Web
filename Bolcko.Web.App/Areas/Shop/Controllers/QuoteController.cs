using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class QuoteController : Controller
    {
        public IActionResult Request()
        {
            return View();
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}