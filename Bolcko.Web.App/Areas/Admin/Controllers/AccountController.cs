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
                    // Force password change if this is the first login (seeded admin)
                    if (user.MustChangePassword)
                        return RedirectToAction("ChangePassword", "Account", new { area = "Admin" });

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

        [HttpGet]
        public IActionResult ChangePassword()
        {
            // If user doesn't need to change password, redirect to dashboard
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "كلمتا المرور غير متطابقتين";
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Remove old password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Clear the force-change flag
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح. مرحباً بك!";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            ViewBag.Error = string.Join("، ", result.Errors.Select(e => e.Description));
            return View();
        }
    }
}
