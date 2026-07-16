using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class DeliveryDispatchController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public DeliveryDispatchController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================
        // DELIVERY JOBS (Dispatch)
        // ============================

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var pagedJobs = await _serviceManager.DeliveryService.GetPagedAllJobsAsync(page, pageSize);
            return View(pagedJobs);
        }

        public async Task<IActionResult> JobDetails(int id)
        {
            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(int orderId, decimal deliveryFee)
        {
            try
            {
                await _serviceManager.DeliveryService.CreateJobForOrderAsync(orderId, deliveryFee);
                TempData["SuccessMessage"] = "تم طرح طلب التوصيل في السوق بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToCompany(int orderId, int companyId, decimal deliveryFee)
        {
            try
            {
                await _serviceManager.DeliveryService.AssignOrderToCompanyAsync(orderId, companyId, deliveryFee);
                TempData["SuccessMessage"] = "تم إسناد الطلب لشركة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            // Redirect to the referring page (usually Order Details or Dispatch Index)
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int jobId, int driverId, decimal fee)
        {
            try
            {
                await _serviceManager.DeliveryService.AssignJobToDriverAsync(jobId, driverId, fee);
                TempData["SuccessMessage"] = "تم تعيين المندوب للطلب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptBid(int bidId, int jobId)
        {
            try
            {
                await _serviceManager.DeliveryService.AcceptBidAsync(bidId);
                TempData["SuccessMessage"] = "تم قبول العرض وتعيين المندوب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJobStatus(int jobId, DeliveryJobStatus status)
        {
            try
            {
                await _serviceManager.DeliveryService.UpdateJobStatusAsync(jobId, status);
                TempData["SuccessMessage"] = "تم تحديث حالة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        // ============================
        // DRIVERS MANAGEMENT
        // ============================

        public async Task<IActionResult> Drivers(int page = 1, int pageSize = 10)
        {
            var pagedDrivers = await _serviceManager.DeliveryService.GetPagedDriversAsync(page, pageSize);
            return View(pagedDrivers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDriver(int driverId)
        {
            try
            {
                await _serviceManager.DeliveryService.ApproveDriverAsync(driverId);
                TempData["SuccessMessage"] = "تم تفعيل حساب المندوب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Drivers");
        }

        // ============================
        // DELIVERY COMPANIES
        // ============================

        public async Task<IActionResult> Companies(int page = 1, int pageSize = 10)
        {
            var pagedCompanies = await _serviceManager.DeliveryService.GetPagedCompaniesAsync(page, pageSize);
            return View(pagedCompanies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(string name, string? email, string? phoneNumber, string? commercialRegister, decimal baseRate, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "يجب إدخال اسم المستخدم وكلمة المرور لمدير الشركة!";
                return RedirectToAction("Companies");
            }

            try
            {
                // 1. Ensure the DeliveryCompanyUser role exists
                const string roleName = "DeliveryCompanyUser";
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });
                }

                // 2. Check if user already exists
                var existingUser = await _userManager.FindByNameAsync(username.Trim());
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "اسم المستخدم لمدير الشركة موجود بالفعل بالسيستم!";
                    return RedirectToAction("Companies");
                }

                // 3. Create the user in Identity
                var user = new User
                {
                    UserName = username.Trim(),
                    Email = email?.Trim() ?? $"{username.Trim()}@bolcko-delivery.com",
                    FirstName = "مدير",
                    LastName = name,
                    UserType = UserType.Customer, // Internal routing compatibility
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"فشل إنشاء حساب المدير: {errors}";
                    return RedirectToAction("Companies");
                }

                // 4. Assign user to DeliveryCompanyUser role
                await _userManager.AddToRoleAsync(user, roleName);

                // 5. Create the DeliveryCompany linked to this user
                await _serviceManager.DeliveryService.CreateCompanyAsync(name, email, phoneNumber, commercialRegister, baseRate, user.Id.ToString());
                TempData["SuccessMessage"] = $"تم إضافة شركة التوصيل '{name}' وحساب المدير بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Companies");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCompanyStatus(int companyId)
        {
            try
            {
                await _serviceManager.DeliveryService.ToggleCompanyStatusAsync(companyId);
                TempData["SuccessMessage"] = "تم تحديث حالة تفعيل شركة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Companies");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCompanyEmail(int jobId, string? email, bool includePdf, bool includeExcel, string? customMessage)
        {
            try
            {
                await _serviceManager.DeliveryService.SendDeliveryDocumentsToCompanyAsync(jobId, email, includePdf, includeExcel, customMessage);
                TempData["SuccessMessage"] = "تم إرسال مستندات التوصيل المطلوبة بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ أثناء الإرسال: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }
    }
}
