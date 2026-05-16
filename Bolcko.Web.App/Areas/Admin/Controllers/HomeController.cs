using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,DashboardUser")]

    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;

        public HomeController(IServiceManager serviceManager,UserManager<User> userManager)
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
