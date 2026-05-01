using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && (User.IsInRole("DashboardUser") || User.IsInRole("Admin")))
            {
                return RedirectToAction("Index", "Home", new { area = "Dashboard" });
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && (user.UserType == UserType.DashboardUser || user.UserType == UserType.Admin))
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return string.IsNullOrEmpty(returnUrl) ? RedirectToAction("Index", "Home", new { area = "Dashboard" }) : LocalRedirect(returnUrl);
                }
            }

            ViewBag.Error = "بيانات الدخول غير صحيحة أو لا تملك صلاحية بائع";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account", new { area = "Dashboard" });
        }
    }
}
