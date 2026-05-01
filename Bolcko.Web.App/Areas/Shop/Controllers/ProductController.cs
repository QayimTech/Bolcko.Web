using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Controllers
{
    [Area("Shop")]
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
  

    }
}
