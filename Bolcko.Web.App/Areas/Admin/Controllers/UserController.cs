using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var usersQuery = _userManager.Users.AsNoTracking().OrderByDescending(u => u.RegistrationDate);
            var totalCount = await usersQuery.CountAsync();
            var items = await usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var pagedResult = new Blocko.Services.Common.PagedList<Bolcko.Domain.Entities.User.User>(items, totalCount, page, pageSize);
            return View(pagedResult);
        }

        public IActionResult CreateAdminUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdminUser(User user, string password, string role = "Admin")
        {
            // We rely on Roles for authorization; keep UserType only for domain semantics.
            user.UserType = role == "Admin" ? UserType.Admin : UserType.DashboardUser;
            user.UserName = user.Email;
            
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();
            
            ViewBag.Roles = new[] { "Admin", "DashboardUser" };
            ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, User user, string? role, string? newPassword)
        {
            var existingUser = await _userManager.FindByIdAsync(id.ToString());
            if (existingUser == null) return NotFound();

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.UserName = user.Email;
            existingUser.CompanyName = user.CompanyName;
            existingUser.BusinessRegistrationNumber = user.BusinessRegistrationNumber;

            if (!string.IsNullOrEmpty(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(existingUser);
                await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                await _userManager.AddToRoleAsync(existingUser, role);
                existingUser.UserType = role == "Admin" ? UserType.Admin : UserType.DashboardUser;
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, token, newPassword);
            }

            var result = await _userManager.UpdateAsync(existingUser);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Roles = new[] { "Admin", "DashboardUser" };
            return View(existingUser);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Index");
        }
    }
}
