using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    [Route("Vendor/[controller]")]
    public class TeamController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;

        public TeamController(BlockoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.VendorProfiles.FirstOrDefaultAsync(v => v.UserId == user.Id);
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? search, VendorRoleType? role)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null)
            {
                TempData["Error"] = "يرجى توثيق ملف المورد الخاص بك للوصول إلى إدارة فريق العمل.";
                return RedirectToAction("Index", "Dashboard");
            }

            var query = _context.VendorTeamMembers
                .Include(t => t.User)
                .Where(t => t.VendorId == vendor.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => t.FullName.ToLower().Contains(s) || t.Email.ToLower().Contains(s) || t.PhoneNumber.Contains(s));
            }

            if (role.HasValue)
            {
                query = query.Where(t => t.RoleType == role.Value);
            }

            var teamMembers = await query.OrderByDescending(t => t.InvitedAt).ToListAsync();

            ViewBag.VendorProfile = vendor;
            ViewBag.TotalCount = await _context.VendorTeamMembers.CountAsync(t => t.VendorId == vendor.Id);
            ViewBag.DataEntryCount = await _context.VendorTeamMembers.CountAsync(t => t.VendorId == vendor.Id && t.RoleType == VendorRoleType.DataEntry);
            ViewBag.WarehouseCount = await _context.VendorTeamMembers.CountAsync(t => t.VendorId == vendor.Id && t.RoleType == VendorRoleType.WarehouseDispatch);
            ViewBag.AccountantCount = await _context.VendorTeamMembers.CountAsync(t => t.VendorId == vendor.Id && t.RoleType == VendorRoleType.Accountant);

            return View(teamMembers);
        }

        [HttpPost]
        [Route("Invite")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(
            string fullName, 
            string email, 
            string phoneNumber, 
            string? jobTitle, 
            VendorRoleType roleType, 
            bool canManageCatalog = false, 
            bool canManageOrders = false, 
            bool canViewFinance = false, 
            bool canManageTeam = false)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return Unauthorized();

            var cleanedEmail = email?.Trim().ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(cleanedEmail))
            {
                TempData["Error"] = "يرجى كتابة الاسم الكامل والبريد الإلكتروني بشكل صحيح.";
                return RedirectToAction(nameof(Index));
            }

            var existingMember = await _context.VendorTeamMembers
                .FirstOrDefaultAsync(t => t.VendorId == vendor.Id && t.Email.ToLower() == cleanedEmail);

            if (existingMember != null)
            {
                TempData["Error"] = "هذا البريد الإلكتروني مسجل بالفعل ضمن فريق عمل منشأتك.";
                return RedirectToAction(nameof(Index));
            }

            // Check or create identity user
            var linkedUser = await _userManager.FindByEmailAsync(cleanedEmail);
            if (linkedUser == null)
            {
                var names = fullName.Trim().Split(' ', 2);
                linkedUser = new User
                {
                    UserName = cleanedEmail,
                    Email = cleanedEmail,
                    PhoneNumber = phoneNumber?.Trim(),
                    FirstName = names.Length > 0 ? names[0] : fullName,
                    LastName = names.Length > 1 ? names[1] : "",
                    UserType = UserType.Vendor,
                    EmailConfirmed = true
                };

                // Generate random temporary secure password
                var tempPassword = $"Blocko@{Guid.NewGuid().ToString("N").Substring(0, 8)}!";
                var createResult = await _userManager.CreateAsync(linkedUser, tempPassword);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(linkedUser, "Vendor");
                }
            }

            // Automatic permission defaults based on selected role type if not manually toggled
            if (roleType == VendorRoleType.VendorAdmin)
            {
                canManageCatalog = true;
                canManageOrders = true;
                canViewFinance = true;
                canManageTeam = true;
            }
            else if (roleType == VendorRoleType.DataEntry)
            {
                canManageCatalog = true;
            }
            else if (roleType == VendorRoleType.WarehouseDispatch)
            {
                canManageOrders = true;
            }
            else if (roleType == VendorRoleType.Accountant)
            {
                canViewFinance = true;
            }

            var teamMember = new VendorTeamMember
            {
                VendorId = vendor.Id,
                UserId = linkedUser?.Id,
                FullName = fullName.Trim(),
                Email = cleanedEmail,
                PhoneNumber = phoneNumber?.Trim() ?? "",
                JobTitle = jobTitle?.Trim(),
                RoleType = roleType,
                CanManageCatalog = canManageCatalog,
                CanManageOrders = canManageOrders,
                CanViewFinance = canViewFinance,
                CanManageTeam = canManageTeam,
                IsActive = true,
                InvitedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow
            };

            _context.VendorTeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تمت إضافة العضو ({fullName}) وتعيين صلاحياته ضمن فريق العمل بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("ToggleStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return Json(new { success = false, message = "غير مصرح." });

            var member = await _context.VendorTeamMembers.FirstOrDefaultAsync(t => t.Id == id && t.VendorId == vendor.Id);
            if (member == null) return Json(new { success = false, message = "العضو غير موجود." });

            member.IsActive = !member.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = member.IsActive, message = member.IsActive ? "تم تفعيل حساب العضو." : "تم تعطيل وصول العضو مؤقتاً." });
        }

        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return Json(new { success = false, message = "غير مصرح." });

            var member = await _context.VendorTeamMembers.FirstOrDefaultAsync(t => t.Id == id && t.VendorId == vendor.Id);
            if (member == null) return Json(new { success = false, message = "العضو غير موجود." });

            _context.VendorTeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم حذف العضو من فريق العمل بنجاح." });
        }
    }
}
