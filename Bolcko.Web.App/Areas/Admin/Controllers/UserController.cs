using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Blocko.Persistence.Common;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly Blocko.Services.Interfaces.IServiceManager _serviceManager;

        public UserController(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, Blocko.Services.Interfaces.IServiceManager serviceManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _serviceManager = serviceManager;
        }

        private async Task LoadRolesToViewBagAsync(bool filterForCreate = false)
        {
            var dbRoles = await _roleManager.Roles
                .Where(r => !string.IsNullOrEmpty(r.Name))
                .Select(r => r.Name!)
                .Distinct()
                .ToListAsync();

            if (filterForCreate)
            {
                // In Create user, show general system roles (Admin, DashboardUser, Customer)
                // Delivery users must be created through Delivery Dispatch with logistics data
                dbRoles = dbRoles.Where(r => r == "Admin" || r == "DashboardUser" || r == "Customer").ToList();
            }

            ViewBag.Roles = dbRoles;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, string? roleFilter = null)
        {
            var usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                var userIdsInRole = (await _userManager.GetUsersInRoleAsync(roleFilter)).Select(u => u.Id).ToList();
                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }

            usersQuery = usersQuery.OrderByDescending(u => u.RegistrationDate);
            var totalCount = await usersQuery.CountAsync();
            var items = await usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var userRoles = new Dictionary<int, string>();
            foreach (var user in items)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? user.UserType.ToString();
            }

            await LoadRolesToViewBagAsync();

            var pagedResult = new PagedList<User>(items, totalCount, page, pageSize);
            var viewModel = new UserIndexViewModel
            {
                Users = pagedResult,
                UserRoles = userRoles,
                Search = search,
                RoleFilter = roleFilter
            };
            return View(viewModel);
        }

        public async Task<IActionResult> CreateAdminUser()
        {
            await LoadRolesToViewBagAsync(filterForCreate: true);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            await LoadRolesToViewBagAsync(filterForCreate: true);
            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            await LoadRolesToViewBagAsync();
            ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            await LoadRolesToViewBagAsync();
            return View(existingUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user != null)
                {
                    // Check if user is a manager for a delivery company
                    var company = await _serviceManager.DeliveryService.GetCompanyByManagerUserIdAsync(user.Id.ToString());
                    if (company != null)
                    {
                        await _serviceManager.DeliveryService.DeleteCompanyAsync(company.Id);
                    }

                    await _userManager.DeleteAsync(user);
                    TempData["SuccessMessage"] = "تم حذف المستخدم وتحديث حالة الشركة المرتبطة به بنجاح.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا المستخدم لأنه مرتبط ببيانات أخرى (مثل طلبات أو أسعار).";
            }
            return RedirectToAction("Index");
        }
    }
}
