using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Web.App.Controllers
{
    [AllowAnonymous]
    public class ImpersonationController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public ImpersonationController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopImpersonation()
        {
            var originalAdminId = HttpContext.Session.GetString("OriginalAdminUserId");
            
            if (!string.IsNullOrEmpty(originalAdminId))
            {
                var originalAdmin = await _userManager.FindByIdAsync(originalAdminId);
                if (originalAdmin != null)
                {
                    await _signInManager.SignInAsync(originalAdmin, isPersistent: false);
                }
            }

            // Clear Impersonation Session state safely
            HttpContext.Session.Remove("OriginalAdminUserId");
            HttpContext.Session.Remove("OriginalAdminUserName");
            HttpContext.Session.Remove("IsImpersonating");
            HttpContext.Session.Remove("IsSuperAdminImpersonating");
            HttpContext.Session.Remove("ImpersonatedUserFullName");
            HttpContext.Session.Remove("ImpersonatedUserRole");

            TempData["SuccessMessage"] = "تم إنهاء وضع المحاكاة والعودة لحساب الأدمن الرئيسي بنجاح.";
            return RedirectToAction("Index", "User", new { area = "Admin" });
        }
    }
}
