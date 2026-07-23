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
        private readonly Blocko.Services.Interfaces.Notifications.INotificationService _notificationService;

        public DeliveryDispatchController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            Blocko.Services.Interfaces.Notifications.INotificationService notificationService)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _notificationService = notificationService;
        }

        // ============================
        // DELIVERY JOBS (Dispatch)
        // ============================

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, DeliveryJobStatus? status = null, DateTime? startDate = null, DateTime? endDate = null, int? companyId = null)
        {
            var pagedJobs = await _serviceManager.DeliveryService.GetPagedAllJobsAsync(page, pageSize, status);
            
            var allJobs = (await _serviceManager.DeliveryService.GetAllJobsAsync()).AsQueryable();

            if (companyId.HasValue)
            {
                allJobs = allJobs.Where(j => j.DeliveryCompanyId == companyId.Value);
            }
            if (startDate.HasValue)
            {
                allJobs = allJobs.Where(j => j.AssignedAt >= startDate.Value || (j.DeliveredAt.HasValue && j.DeliveredAt >= startDate.Value));
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                allJobs = allJobs.Where(j => j.AssignedAt <= endOfDay || (j.DeliveredAt.HasValue && j.DeliveredAt <= endOfDay));
            }

            var jobsList = allJobs.ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCompanyId = companyId;
            ViewBag.DeliveryCompanies = await _serviceManager.DeliveryService.GetActiveCompaniesAsync();

            // Dynamic KPIs based on selected date range & company filter
            ViewBag.KpiTotalJobs = jobsList.Count;
            ViewBag.KpiAvailableJobs = jobsList.Count(j => j.Status == DeliveryJobStatus.Available);
            ViewBag.KpiInTransitJobs = jobsList.Count(j => j.Status == DeliveryJobStatus.InTransit || j.Status == DeliveryJobStatus.PickedUp || j.Status == DeliveryJobStatus.Assigned);
            ViewBag.KpiDeliveredJobs = jobsList.Count(j => j.Status == DeliveryJobStatus.Delivered);

            var totalCollected = jobsList.Where(j => j.Status == DeliveryJobStatus.Delivered).Sum(j => j.CollectedAmount ?? 0);
            var totalFees = jobsList.Where(j => j.Status == DeliveryJobStatus.Delivered || j.Status == DeliveryJobStatus.Returned).Sum(j => j.DeliveryFee);

            ViewBag.KpiTotalCollected = totalCollected;
            ViewBag.KpiTotalFees = totalFees;
            ViewBag.KpiNetSales = totalCollected - totalFees; // صافي المبيعات بعد خصم أجور التوصيل

            // Prepare Daily Trend Chart Data (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .ToList();

            var dailyLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            var dailyDelivered = last7Days.Select(d => jobsList.Count(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == DeliveryJobStatus.Delivered)).ToList();
            var dailyCollected = last7Days.Select(d => jobsList.Where(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == DeliveryJobStatus.Delivered).Sum(j => j.CollectedAmount ?? 0)).ToList();

            ViewBag.DailyLabelsJson = System.Text.Json.JsonSerializer.Serialize(dailyLabels);
            ViewBag.DailyDeliveredJson = System.Text.Json.JsonSerializer.Serialize(dailyDelivered);
            ViewBag.DailyCollectedJson = System.Text.Json.JsonSerializer.Serialize(dailyCollected);

            // Status Distribution Doughnut Data
            var statusCounts = new[]
            {
                jobsList.Count(j => j.Status == DeliveryJobStatus.Available),
                jobsList.Count(j => j.Status == DeliveryJobStatus.Assigned || j.Status == DeliveryJobStatus.PickedUp || j.Status == DeliveryJobStatus.InTransit),
                jobsList.Count(j => j.Status == DeliveryJobStatus.Delivered),
                jobsList.Count(j => j.Status == DeliveryJobStatus.Returned)
            };
            ViewBag.StatusCountsJson = System.Text.Json.JsonSerializer.Serialize(statusCounts);

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

                // Notify company manager user
                var company = await _serviceManager.DeliveryService.GetCompanyByIdAsync(companyId);
                if (company != null && !string.IsNullOrEmpty(company.ManagerUserId) && int.TryParse(company.ManagerUserId, out int managerUserId))
                {
                    await _notificationService.SendNotificationToUserAsync(
                        managerUserId,
                        "تم إسناد شحنة جديدة لشركتك",
                        $"تم إسناد الطلب رقم #{orderId} لشركتك. يمكنك الآن توزيع الشحنة على مندوبيك.",
                        "/Delivery/Home"
                    );
                }

                TempData["SuccessMessage"] = "تم إسناد الطلب لشركة التوصيل وإشعارها بنجاح!";
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
        public async Task<IActionResult> DeleteCompany(int companyId)
        {
            try
            {
                var company = await _serviceManager.DeliveryService.GetCompanyByIdAsync(companyId);
                if (company != null)
                {
                    if (!string.IsNullOrEmpty(company.ManagerUserId) && int.TryParse(company.ManagerUserId, out int managerId))
                    {
                        var user = await _userManager.FindByIdAsync(managerId.ToString());
                        if (user != null)
                        {
                            await _userManager.DeleteAsync(user);
                        }
                    }

                    await _serviceManager.DeliveryService.DeleteCompanyAsync(companyId);
                    TempData["SuccessMessage"] = "تم حذف/تعطيل شركة التوصيل وحساب مديرها بنجاح!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ أثناء الحذف: {ex.Message}";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFreelanceDriver(string firstName, string lastName, string phoneNumber, string? email, string password, string vehicleType, string? vehiclePlateNumber, string? licenseNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "الرجاء إدخال الاسم ورقم الهاتف وكلمة المرور بشكل صحيح.";
                    return RedirectToAction("Drivers");
                }

                string emailToUse = !string.IsNullOrWhiteSpace(email) 
                    ? email.Trim() 
                    : $"driver_{phoneNumber.Trim()}@bolcko-delivery.com";

                var existingUser = await _userManager.FindByEmailAsync(emailToUse);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "يوجد حساب بريد إلكتروني مسجل بهذا العنوان بالفعل.";
                    return RedirectToAction("Drivers");
                }

                var user = new User
                {
                    UserName = phoneNumber.Trim(),
                    Email = emailToUse,
                    FirstName = firstName.Trim(),
                    LastName = lastName?.Trim() ?? "",
                    PhoneNumber = phoneNumber.Trim(),
                    UserType = UserType.Customer,
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"فشل إنشاء حساب المستخدم: {errors}";
                    return RedirectToAction("Drivers");
                }

                const string roleName = "DeliveryDriver";
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
                await _userManager.AddToRoleAsync(user, roleName);

                // Register Driver entity (companyId: null -> Freelance Driver)
                var driver = await _serviceManager.DeliveryService.RegisterDriverAsync(user.Id, null, vehicleType, vehiclePlateNumber, licenseNumber);
                
                // Auto-approve driver created by Admin
                await _serviceManager.DeliveryService.ApproveDriverAsync(driver.Id);

                TempData["SuccessMessage"] = $"تم إنشاء وتفعيل حساب المندوب الحر '{firstName} {lastName}' بنجاح! 🎯";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ أثناء إضافة المندوب الحر: {ex.Message}";
            }
            return RedirectToAction("Drivers");
        }
    }
}
