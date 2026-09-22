using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area("Delivery")]
    [Authorize(Roles = "DeliveryDriver, DeliveryCompanyUser, SuperAdmin, Admin")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly Blocko.Services.Interfaces.Notifications.INotificationService _notificationService;
        private readonly Bolcko.Domain.Interfaces.IUnitOfWork _unitOfWork;

        public HomeController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            Blocko.Services.Interfaces.Notifications.INotificationService notificationService,
            Bolcko.Domain.Interfaces.IUnitOfWork unitOfWork)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult PendingApproval()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return RedirectToAction("Register", "Account", new { area = "Delivery" });
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Handle Delivery Company Manager login
            if (await _userManager.IsInRoleAsync(user, "DeliveryCompanyUser"))
            {
                return await LoadCompanyDashboard(user, startDate, endDate);
            }

            // 2. Handle Delivery Driver login
            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null)
            {
                TempData["Info"] = "لم يتم ربط حسابك بملف ناقل أو كابتن بعد. يرجى إكمال التسجيل أولاً.";
                return RedirectToAction("Register", "Account", new { area = "Delivery" });
            }

            if (!driver.IsApproved && !(await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin")))
            {
                TempData["Warning"] = "حسابك قيد المراجعة والتدقيق لدى إدارة العمليات اللوجستية (KYC Pending). سيتم إشعارك فور اعتماد الأوراق.";
                return RedirectToAction("PendingApproval");
            }

            var myJobs = await _serviceManager.DeliveryService.GetDriverJobsAsync(driver.Id);
            var availableJobs = await _serviceManager.DeliveryService.GetAvailableJobsAsync();
            var myBids = await _serviceManager.DeliveryService.GetDriverBidsAsync(driver.Id);

            ViewBag.Driver = driver;
            ViewBag.AvailableJobs = availableJobs;
            ViewBag.MyBids = myBids;
            return View(myJobs);
        }

        [HttpGet]
        public async Task<IActionResult> CompanyIndex(DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            return await LoadCompanyDashboard(user, startDate, endDate);
        }

        private async Task<IActionResult> LoadCompanyDashboard(User user, DateTime? startDate, DateTime? endDate)
        {
            var company = await _serviceManager.DeliveryService.GetCompanyByManagerUserIdAsync(user.Id.ToString());
            if (company == null)
            {
                if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var allCompanies = await _serviceManager.DeliveryService.GetAllCompaniesAsync();
                    company = allCompanies.FirstOrDefault();
                }

                if (company == null)
                {
                    var companyName = !string.IsNullOrWhiteSpace(user.CompanyName) ? user.CompanyName : $"{user.FirstName} {user.LastName} للشحن";
                    company = await _serviceManager.DeliveryService.CreateCompanyAsync(companyName, user.Email, user.PhoneNumber, "200189422", 25.00m, user.Id.ToString(), isApproved: true);
                }
            }

            if (!company.IsApproved && !(await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin")))
            {
                TempData["Warning"] = "ملف شركة الشحن والأسطول قيد التدقيق القانوني (KYC Verification). سيتم تفعيل حسابكم فور المصادقة على السجل التجاري وترخيص هيئة النقل البري.";
                return RedirectToAction("PendingApproval");
            }

            var jobsQuery = (await _serviceManager.DeliveryService.GetCompanyJobsAsync(company.Id)).AsQueryable();

            if (startDate.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.AssignedAt >= startDate.Value || (j.DeliveredAt.HasValue && j.DeliveredAt >= startDate.Value));
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                jobsQuery = jobsQuery.Where(j => j.AssignedAt <= endOfDay || (j.DeliveredAt.HasValue && j.DeliveredAt <= endOfDay));
            }

            var jobs = jobsQuery.ToList();

            // Compute Financial statistics
            ViewBag.Company = company;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            ViewBag.TotalJobs = jobs.Count;
            ViewBag.ActiveJobs = jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Assigned || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.InTransit);
            ViewBag.DeliveredJobs = jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered);
            
            // COD collected & fees calculations
            ViewBag.TotalCollected = jobs.Where(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered).Sum(j => j.CollectedAmount ?? 0);
            ViewBag.TotalFees = jobs.Where(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned).Sum(j => j.DeliveryFee);
            ViewBag.UnreconciledAmount = jobs.Where(j => !j.IsReconciled && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered).Sum(j => (j.CollectedAmount ?? 0) - j.DeliveryFee);

            // Prepare Chart Data for Company Dashboard
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .ToList();

            var dailyLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            var dailyDelivered = last7Days.Select(d => jobs.Count(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered)).ToList();
            var dailyCollected = last7Days.Select(d => jobs.Where(j => j.DeliveredAt.HasValue && j.DeliveredAt.Value.Date == d && j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered).Sum(j => j.CollectedAmount ?? 0)).ToList();

            ViewBag.DailyLabelsJson = System.Text.Json.JsonSerializer.Serialize(dailyLabels);
            ViewBag.DailyDeliveredJson = System.Text.Json.JsonSerializer.Serialize(dailyDelivered);
            ViewBag.DailyCollectedJson = System.Text.Json.JsonSerializer.Serialize(dailyCollected);

            var statusCounts = new[]
            {
                jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Available),
                jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Assigned || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp || j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.InTransit),
                jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Delivered),
                jobs.Count(j => j.Status == Bolcko.Domain.Enums.DeliveryJobStatus.Returned)
            };
            ViewBag.StatusCountsJson = System.Text.Json.JsonSerializer.Serialize(statusCounts);

            var companyDrivers = await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Where(d => d.DeliveryCompanyId == company.Id)
                .ToListAsync();

            ViewBag.CompanyDrivers = companyDrivers;

            return View("CompanyIndex", jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompanyCollection(int jobId, decimal collectedAmount, string? returnReason)
        {
            try
            {
                await _serviceManager.DeliveryService.UpdateCompanyJobCollectedAmountAsync(jobId, collectedAmount, returnReason);
                TempData["Success"] = "تم تحديث حالة الشحنة والتحصيل المالي بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء التحديث: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBid(int jobId, decimal bidAmount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return BadRequest();

            try
            {
                await _serviceManager.DeliveryService.PlaceBidAsync(jobId, driver.Id, bidAmount);
                TempData["Success"] = "تم تقديم عرضك بنجاح! سيتم إشعارك عند قبوله.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int jobId, Bolcko.Domain.Enums.DeliveryJobStatus status)
        {
            try
            {
                await _serviceManager.DeliveryService.UpdateJobStatusAsync(jobId, status);

                // Notify Delivery Company Manager if applicable
                var job = await _serviceManager.DeliveryService.GetJobByIdAsync(jobId);
                if (job != null && job.DeliveryCompanyId.HasValue)
                {
                    var company = await _serviceManager.DeliveryService.GetCompanyByIdAsync(job.DeliveryCompanyId.Value);
                    if (company != null && !string.IsNullOrEmpty(company.ManagerUserId) && int.TryParse(company.ManagerUserId, out int managerUserId))
                    {
                        var statusName = status switch
                        {
                            Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp  => "تم استلام الشحنة",
                            Bolcko.Domain.Enums.DeliveryJobStatus.InTransit => "قيد التوصيل",
                            Bolcko.Domain.Enums.DeliveryJobStatus.Delivered => "تم التسليم للزبون",
                            Bolcko.Domain.Enums.DeliveryJobStatus.Returned  => "مرتجع",
                            _ => status.ToString()
                        };

                        await _notificationService.SendNotificationToUserAsync(
                            managerUserId,
                            "تحديث حالة شحنة",
                            $"قام المندوب بتحديث حالة الشحنة للطلب #{job.OrderId} إلى: {statusName}.",
                            "/Delivery/Home"
                        );
                    }
                }

                TempData["Success"] = "تم تحديث الحالة وإشعار شركة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return BadRequest();

            await _serviceManager.DeliveryService.UpdateDriverAvailabilityAsync(driver.Id, !driver.IsAvailable);
            TempData["Success"] = driver.IsAvailable ? "تم تغيير حالتك إلى غير متاح (في استراحة)." : "تم تغيير حالتك إلى متاح (جاهز لتلقي الحمولات على الرادار) 🟢";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptLoad(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null || !driver.IsApproved)
            {
                TempData["Error"] = "حسابك غير مؤهل أو بانتظار الاعتماد الإداري.";
                return RedirectToAction("Index");
            }

            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(jobId);
            if (job == null || (job.Status != Bolcko.Domain.Enums.DeliveryJobStatus.Available && job.DriverId != null))
            {
                TempData["Error"] = "عذراً، هذه الحمولة تم حجزها من كابتن آخر أو لم تعد متاحة.";
                return RedirectToAction("Index");
            }

            job.DriverId = driver.Id;
            job.AssignedAt = DateTime.UtcNow;
            job.Status = Bolcko.Domain.Enums.DeliveryJobStatus.Assigned;
            if (string.IsNullOrEmpty(job.DeliveryOtpCode))
            {
                job.DeliveryOtpCode = new Random().Next(100000, 999999).ToString();
            }

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = $"تم قبول الحمولة للطلب #{job.OrderId} بنجاح! توجه الآن إلى مستودع التحميل وقم بوزن الشاحنة وتصوير تذكرة القبان.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadWeighbridge(int jobId, IFormFile? weighbridgePhoto, decimal? grossWeight, decimal? tareWeight)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return Unauthorized();

            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(jobId);
            if (job == null || job.DriverId != driver.Id)
            {
                TempData["Error"] = "المهمة غير موجودة أو غير مسندة إليك.";
                return RedirectToAction("Index");
            }

            if (weighbridgePhoto == null || weighbridgePhoto.Length == 0)
            {
                TempData["Error"] = "يرجى تصوير وإرفاق صورة واضحة لتذكرة القبان الرسمية عند بوابة الخروج.";
                return RedirectToAction("Index");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var ext = Path.GetExtension(weighbridgePhoto.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "صيغة الملف غير مدعومة. يرجى إرفاق صورة عادية (JPG/PNG).";
                return RedirectToAction("Index");
            }

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "weighbridge");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = $"wb_{jobId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await weighbridgePhoto.CopyToAsync(stream);
            }

            job.WeighbridgeTicketUrl = $"/uploads/weighbridge/{fileName}";
            job.GrossWeightTons = grossWeight ?? 0;
            job.TareWeightTons = tareWeight ?? 0;
            job.WeighedAt = DateTime.UtcNow;
            job.Status = Bolcko.Domain.Enums.DeliveryJobStatus.InTransit; // Transitions to EnRoute / InTransit

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            TempData["Success"] = "تم توثيق تذكرة القبان بنجاح! انتقلت الشاحنة الآن إلى حالة قيد التوصيل (InTransit) باتجاه الورشة. 🚛";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyDeliveryOtp(int jobId, string otpCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return Unauthorized();

            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(jobId);
            if (job == null || job.DriverId != driver.Id)
            {
                TempData["Error"] = "المهمة غير موجودة.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(otpCode) || (job.DeliveryOtpCode != null && job.DeliveryOtpCode.Trim() != otpCode.Trim()))
            {
                TempData["Error"] = "رمز التسليم الرقمي (e-POD OTP) غير صحيح! اطلب الرمز المكون من 6 أرقام من المقاول أو المشرف في الورشة.";
                return RedirectToAction("Index");
            }

            job.Status = Bolcko.Domain.Enums.DeliveryJobStatus.Delivered;
            job.DeliveredAt = DateTime.UtcNow;
            job.IsPodVerified = true;
            job.PodVerifiedAt = DateTime.UtcNow;

            driver.TotalDeliveredOrders += 1;
            _unitOfWork.DeliveryDrivers.Update(driver);
            _unitOfWork.DeliveryJobs.Update(job);

            await _unitOfWork.CompleteAsync();

            TempData["Success"] = $"تم إثبات التسليم الرقمي e-POD بنجاح! تم إيداع أتعاب التوصيل ({job.DeliveryFee:F2} د.أ) إلى محفظة CliQ الخاصة بك.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriverToJob(int jobId, int driverId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var company = await _serviceManager.DeliveryService.GetCompanyByManagerUserIdAsync(user.Id.ToString());
            if (company == null) return Unauthorized();

            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();

            var driver = await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == driverId && d.DeliveryCompanyId == company.Id);

            if (driver == null)
            {
                TempData["Error"] = "السائق المحدد غير مسجل ضمن أسطول شركتك.";
                return RedirectToAction("CompanyIndex");
            }

            job.DriverId = driver.Id;
            job.DeliveryCompanyId = company.Id;
            job.AssignedAt = DateTime.UtcNow;
            job.Status = Bolcko.Domain.Enums.DeliveryJobStatus.Assigned;
            if (string.IsNullOrEmpty(job.DeliveryOtpCode))
            {
                job.DeliveryOtpCode = new Random().Next(100000, 999999).ToString();
            }

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            await _notificationService.SendNotificationToUserAsync(
                driver.UserId,
                "مهمة نقل جديدة مسندة 🚛",
                $"قامت إدارة أسطول '{company.Name}' بإسناد حمولة جديدة لك (طلب #{job.OrderId}) لنقل {job.MaterialType ?? "مواد إنشائية"}. يرجى التوجه لساحة التحميل."
            );

            TempData["Success"] = $"تم إسناد الشحنة بنجاح إلى السائق '{driver.User?.FirstName} {driver.User?.LastName}' وتوجيهه للتحميل!";
            return RedirectToAction("CompanyIndex");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFleetTruck(string driverName, string phone, string vehicleType, string plateNumber, int capacityTons)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var company = await _serviceManager.DeliveryService.GetCompanyByManagerUserIdAsync(user.Id.ToString());
            if (company == null) return Unauthorized();

            var nameParts = (driverName ?? "سائق أسطول").Trim().Split(' ');
            var firstName = nameParts.Length > 0 ? nameParts[0] : "سائق";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "أسطول";

            var randomSuffix = Guid.NewGuid().ToString("N")[..8];
            var driverUser = new User
            {
                UserName = $"fleet_{randomSuffix}@bolcko.jo",
                Email = $"fleet_{randomSuffix}@bolcko.jo",
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone?.Trim(),
                UserType = UserType.DeliveryDriver,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow
            };

            var res = await _userManager.CreateAsync(driverUser, "FleetTruck@123");
            if (res.Succeeded)
            {
                await _userManager.AddToRoleAsync(driverUser, "DeliveryDriver");
                await _serviceManager.DeliveryService.RegisterDriverAsync(
                    userId: driverUser.Id,
                    companyId: company.Id,
                    vehicleType: vehicleType ?? "تريلا مقطورات 30-40 طن",
                    vehiclePlateNumber: plateNumber ?? "12-38491",
                    licenseNumber: "LTRC-DRV-" + driverUser.Id,
                    capacityTons: capacityTons > 0 ? capacityTons : 20,
                    coveredGovernorate: "كافة المحافظات"
                );

                var createdDriver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(driverUser.Id);
                if (createdDriver != null)
                {
                    createdDriver.IsApproved = true;
                    createdDriver.ApprovedAt = DateTime.UtcNow;
                    _unitOfWork.DeliveryDrivers.Update(createdDriver);
                    await _unitOfWork.CompleteAsync();
                }

                TempData["Success"] = $"تمت إضافة الشاحنة والسائق '{driverName}' إلى سجل أسطول الشركة بنجاح!";
            }
            else
            {
                TempData["Error"] = res.Errors.FirstOrDefault()?.Description ?? "تعذر إضافة الشاحنة للأسطول.";
            }

            return RedirectToAction("CompanyIndex");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string vehicleType, string? vehiclePlateNumber, string? licenseNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                await _serviceManager.DeliveryService.RegisterDriverAsync(user.Id, null, vehicleType, vehiclePlateNumber, licenseNumber);
                TempData["Success"] = "تم تسجيل طلبك بنجاح! سيتم مراجعته من الإدارة.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPickup(int jobId)
        {
            try
            {
                await _serviceManager.DeliveryService.AcceptCompanyPickupAsync(jobId);
                TempData["Success"] = "تم استلام الشحنة من المستودع بنجاح وهي الآن قيد التوصيل!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء استلام الشحنة: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> DriverUpdate(string token)
        {
            var job = await _serviceManager.DeliveryService.GetJobByTokenAsync(token);
            if (job == null)
            {
                return NotFound("رابط تتبع السائق غير صالح أو منتهي الصلاحية.");
            }
            return View(job);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDriverUpdate(string token, decimal collectedAmount, string? returnReason)
        {
            var job = await _serviceManager.DeliveryService.GetJobByTokenAsync(token);
            if (job == null) return NotFound();

            try
            {
                await _serviceManager.DeliveryService.UpdateCompanyJobCollectedAmountAsync(job.Id, collectedAmount, returnReason);
                ViewBag.SuccessMessage = "تم تحديث حالة الشحنة والتحصيل المالي بنجاح! شكراً لك.";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"حدث خطأ أثناء التحديث: {ex.Message}";
            }

            return View("DriverUpdate", job);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDispatchSheet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var company = await _serviceManager.DeliveryService.GetCompanyByManagerUserIdAsync(user.Id.ToString());
            if (company == null) return BadRequest("حسابك غير مرتبط بشركة شحن.");

            var jobs = (await _serviceManager.DeliveryService.GetCompanyJobsAsync(company.Id)).ToList();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("رقم الطلب,تاريخ الإسناد,اسم الزبون,المدينة,العنوان,الحالة,المبلغ المحصل (COD),أجرة الشحن,حالة التسوية");

            foreach (var job in jobs)
            {
                var statusStr = job.Status switch
                {
                    Bolcko.Domain.Enums.DeliveryJobStatus.Assigned  => "مُسند",
                    Bolcko.Domain.Enums.DeliveryJobStatus.PickedUp  => "تم الاستلام من المستودع",
                    Bolcko.Domain.Enums.DeliveryJobStatus.InTransit => "قيد التوصيل",
                    Bolcko.Domain.Enums.DeliveryJobStatus.Delivered => "تم التسليم",
                    Bolcko.Domain.Enums.DeliveryJobStatus.Returned  => "مرتجع",
                    Bolcko.Domain.Enums.DeliveryJobStatus.Cancelled => "ملغي",
                    _ => job.Status.ToString()
                };

                var customerName = job.Order?.User != null ? $"{job.Order.User.FirstName} {job.Order.User.LastName}".Trim() : "زبون عام";
                var city = job.Order?.ShippingAddress?.City ?? "";
                var address = (job.Order?.ShippingAddress?.AddressLine1 ?? "").Replace(",", " ");
                var collected = job.CollectedAmount?.ToString("F2") ?? "0.00";
                var fee = job.DeliveryFee.ToString("F2");
                var reconciled = job.IsReconciled ? "تمت التسوية" : "معلق";
                var assignedDate = job.AssignedAt.HasValue ? job.AssignedAt.Value.ToString("yyyy-MM-dd HH:mm") : "";

                builder.AppendLine($"#{job.OrderId},{assignedDate},\"{customerName}\",\"{city}\",\"{address}\",\"{statusStr}\",{collected},{fee},\"{reconciled}\"");
            }

            var csvBytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            var fileName = $"Dispatch_Sheet_{company.Name}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
