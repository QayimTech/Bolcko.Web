using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult Quotes()
        {
            return View();
        }

        public IActionResult Projects()
        {
            return View();
        }
    }
}