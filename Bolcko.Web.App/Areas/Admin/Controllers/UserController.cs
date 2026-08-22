using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.User.DTOs;
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

        // =========================================================================
        // Dynamic Role & Permission Switch Matrix Management
        // =========================================================================

        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var roleDtos = new List<RoleDetailsDto>();

            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                roleDtos.Add(new RoleDetailsDto
                {
                    Id = role.Id,
                    RoleName = role.Name!,
                    UsersCount = usersInRole.Count,
                    SelectedPermissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
                });
            }

            ViewBag.AllPermissionGroups = Bolcko.Domain.Common.AppPermissions.GetAllPermissionGroups();
            return View(roleDtos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(CreateUpdateRoleDto model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleName))
            {
                TempData["ErrorMessage"] = "اسم الدور مطلوب.";
                return RedirectToAction("Roles");
            }

            IdentityRole<int>? role = null;
            if (model.RoleId.HasValue && model.RoleId.Value > 0)
            {
                role = await _roleManager.FindByIdAsync(model.RoleId.Value.ToString());
            }

            if (role == null)
            {
                role = new IdentityRole<int>(model.RoleName.Trim());
                var createRes = await _roleManager.CreateAsync(role);
                if (!createRes.Succeeded)
                {
                    TempData["ErrorMessage"] = "تعذر إنشاء الدور. قد يكون الاسم مكرراً.";
                    return RedirectToAction("Roles");
                }
            }
            else
            {
                role.Name = model.RoleName.Trim();
                await _roleManager.UpdateAsync(role);
            }

            // Sync Permission Claims
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            if (model.SelectedPermissions != null)
            {
                foreach (var permissionKey in model.SelectedPermissions)
                {
                    await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", permissionKey));
                }
            }

            TempData["SuccessMessage"] = $"تم حفظ الدور ({role.Name}) والسويتشات بنجاح.";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role != null)
            {
                if (role.Name == "Admin" || role.Name == "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف الأدوار الرئيسية للنظام.";
                    return RedirectToAction("Roles");
                }

                await _roleManager.DeleteAsync(role);
                TempData["SuccessMessage"] = "تم حذف الدور بنجاح.";
            }
            return RedirectToAction("Roles");
        }

        // =========================================================================
        // Secure Impersonation Feature (SuperAdmin Only Rule)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImpersonateUser(int userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _userManager.GetUserAsync(User);

            // Rule 1: SuperAdmin Only check
            if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                TempData["ErrorMessage"] = "غير مصرح لك باستخدام ميزة المحاكاة. هذه الميزة مخصصة للـ SuperAdmin فقط.";
                return RedirectToAction("Index");
            }

            var targetUser = await _userManager.FindByIdAsync(userId.ToString());
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "المستخدم المراد محاكاته غير موجود.";
                return RedirectToAction("Index");
            }

            // Rule 2: Prevent Impersonation of other SuperAdmins
            if (await _userManager.IsInRoleAsync(targetUser, "Admin"))
            {
                TempData["ErrorMessage"] = "أمنياً: يمنع محاكاة حسابات الـ SuperAdmin الآخرين.";
                return RedirectToAction("Index");
            }

            // Rule 3: Check target role for proper safe redirection
            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            var isDeliveryUser = targetRoles.Contains("DeliveryCompanyUser") || targetRoles.Contains("DeliveryDriver");
            var isCustomer = targetRoles.Contains("Customer") || (!isDeliveryUser && targetUser.UserType == UserType.Customer);

            // Store original Admin state securely in Session
            HttpContext.Session.SetString("OriginalAdminUserId", currentUserId!);
            HttpContext.Session.SetString("OriginalAdminUserName", currentUser.UserName ?? currentUser.Email ?? "Admin");
            HttpContext.Session.SetString("IsImpersonating", "true");
            HttpContext.Session.SetString("IsSuperAdminImpersonating", "true");
            HttpContext.Session.SetString("ImpersonatedUserFullName", $"{targetUser.FirstName} {targetUser.LastName}");
            HttpContext.Session.SetString("ImpersonatedUserRole", targetRoles.FirstOrDefault() ?? targetUser.UserType.ToString());

            // SignIn as Target User
            var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
            await signInManager.SignInAsync(targetUser, isPersistent: false);

            TempData["SuccessMessage"] = $"أنت تتصفح المنصة الآن بوضع المحاكاة كـ ({targetUser.FirstName} {targetUser.LastName}).";

            if (isDeliveryUser)
            {
                return RedirectToAction("Index", "Home", new { area = "Shop" });
            }

            return RedirectToAction("Index", "Home", new { area = "Shop" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopImpersonation()
        {
            var originalAdminId = HttpContext.Session.GetString("OriginalAdminUserId");
            if (string.IsNullOrEmpty(originalAdminId))
            {
                return RedirectToAction("Index", "Home", new { area = "Shop" });
            }

            var originalAdmin = await _userManager.FindByIdAsync(originalAdminId);
            if (originalAdmin != null)
            {
                var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
                await signInManager.SignInAsync(originalAdmin, isPersistent: false);
            }

            // Clear Session Impersonation keys
            HttpContext.Session.Remove("OriginalAdminUserId");
            HttpContext.Session.Remove("OriginalAdminUserName");
            HttpContext.Session.Remove("IsImpersonating");
            HttpContext.Session.Remove("ImpersonatedUserFullName");

            TempData["SuccessMessage"] = "تم إنهاء وضع المحاكاة والعودة لحساب الأدمن الرئيسي بنجاح.";
            return RedirectToAction("Index", "User", new { area = "Admin" });
        }
    }
}
