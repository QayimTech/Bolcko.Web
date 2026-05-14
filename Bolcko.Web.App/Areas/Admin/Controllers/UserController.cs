using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public IActionResult CreateAdminUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdminUser(User user, string password)
        {
            // We rely on Roles for authorization; keep UserType only for domain semantics.
            user.UserType = UserType.Admin;
            user.UserName = user.Email;
            
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "AdminUser");
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(user);
        }
    }
}
