using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
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
            if (User.Identity?.IsAuthenticated == true && (User.IsInRole("Admin") || User.IsInRole("DashboardUser")))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null &&
                (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "DashboardUser")))
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return string.IsNullOrEmpty(returnUrl) ? RedirectToAction("Index", "Home", new { area = "Admin" }) : LocalRedirect(returnUrl);
                }
            }

            ViewBag.Error = "بيانات الدخول غير صحيحة أو لا تملك صلاحية مسؤول";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
    }
}
