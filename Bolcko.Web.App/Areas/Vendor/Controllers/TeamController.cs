using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public TeamController(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            var list = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            return list.FirstOrDefault();
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

            var allMembers = (await _unitOfWork.VendorTeamMembers.FindAsync(t => t.VendorId == vendor.Id)).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                allMembers = allMembers.Where(t => t.FullName.ToLower().Contains(s) || t.Email.ToLower().Contains(s) || t.PhoneNumber.Contains(s)).ToList();
            }

            if (role.HasValue)
            {
                allMembers = allMembers.Where(t => t.RoleType == role.Value).ToList();
            }

            var teamMembers = allMembers.OrderByDescending(t => t.InvitedAt).ToList();

            var totalMembers = await _unitOfWork.VendorTeamMembers.FindAsync(t => t.VendorId == vendor.Id);
            var totalList = totalMembers.ToList();

            ViewBag.VendorProfile = vendor;
            ViewBag.TotalCount = totalList.Count;
            ViewBag.DataEntryCount = totalList.Count(t => t.RoleType == VendorRoleType.DataEntry);
            ViewBag.WarehouseCount = totalList.Count(t => t.RoleType == VendorRoleType.WarehouseDispatch);
            ViewBag.AccountantCount = totalList.Count(t => t.RoleType == VendorRoleType.Accountant);

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

            var existingList = await _unitOfWork.VendorTeamMembers.FindAsync(t => t.VendorId == vendor.Id && t.Email.ToLower() == cleanedEmail);
            var existingMember = existingList.FirstOrDefault();

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

            await _unitOfWork.VendorTeamMembers.AddAsync(teamMember);
            await _unitOfWork.CompleteAsync();

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

            var list = await _unitOfWork.VendorTeamMembers.FindAsync(t => t.Id == id && t.VendorId == vendor.Id);
            var member = list.FirstOrDefault();
            if (member == null) return Json(new { success = false, message = "العضو غير موجود." });

            member.IsActive = !member.IsActive;
            _unitOfWork.VendorTeamMembers.Update(member);
            await _unitOfWork.CompleteAsync();

            return Json(new { success = true, isActive = member.IsActive, message = member.IsActive ? "تم تفعيل حساب العضو." : "تم تعطيل وصول العضو مؤقتاً." });
        }

        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return Json(new { success = false, message = "غير مصرح." });

            var list = await _unitOfWork.VendorTeamMembers.FindAsync(t => t.Id == id && t.VendorId == vendor.Id);
            var member = list.FirstOrDefault();
            if (member == null) return Json(new { success = false, message = "العضو غير موجود." });

            _unitOfWork.VendorTeamMembers.Remove(member);
            await _unitOfWork.CompleteAsync();

            return Json(new { success = true, message = "تم حذف العضو من فريق العمل بنجاح." });
        }
    }
}
