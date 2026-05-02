using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> _userManager;

        public HomeController(IServiceManager serviceManager, Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> userManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserCount = _userManager.Users.Count();
            ViewBag.ProductCount = (await _serviceManager.ProductService.GetAllProductsAsync()).Count();
            ViewBag.CategoryCount = (await _serviceManager.CategoryService.GetAllCategoriesAsync()).Count();
            return View();
        }
    }
}
