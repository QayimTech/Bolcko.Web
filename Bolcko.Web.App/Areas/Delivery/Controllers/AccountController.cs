using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Bolcko.Domain.Entities.User;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area("Delivery")]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IServiceManager _serviceManager;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IServiceManager serviceManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _serviceManager = serviceManager;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && (await _userManager.IsInRoleAsync(user, "DeliveryCompanyUser") || await _userManager.IsInRoleAsync(user, "DeliveryDriver")))
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home", new { area = "Delivery" });
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { area = "Delivery" });
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string? companyName,
            string phone,
            string email,
            string password,
            string? registrationType = "captain",
            // Freelance Captain Heavy Transport KYC
            string? nationalId = null,
            string? heavyLicenseCategory = null,
            string? vehicleType = null,
            string? plateNumber = null,
            int? capacityTons = 15,
            string? coveredGovernorate = null,
            IFormFile? licenseDoc = null,
            IFormFile? registrationDoc = null,
            IFormFile? vehiclePhoto = null,
            string? driverCliqAlias = null,
            // 3PL Fleet Company Carrier KYC
            string? commercialRegister = null,
            string? taxId = null,
            string? transportCommissionLicense = null,
            int? totalTrucksCount = 5,
            IFormFile? commercialRegisterDoc = null,
            IFormFile? transportLicenseDoc = null,
            string? companyCliqAlias = null,
            string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "يرجى ملء كافة الحقول الأساسية المطلوبة.";
                return View();
            }

            var existing = await _userManager.FindByEmailAsync(email.Trim());
            if (existing != null)
            {
                ViewBag.Error = "هذا البريد الإلكتروني مسجل مسبقاً في المنصة.";
                return View();
            }

            var nameParts = fullName.Trim().Split(' ');
            var firstName = nameParts.Length > 0 ? nameParts[0] : "سائق";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "أسطول";

            bool isCompany = registrationType == "company" || !string.IsNullOrWhiteSpace(companyName);

            var user = new User
            {
                UserName = email.Trim(),
                Email = email.Trim(),
                PhoneNumber = phone?.Trim(),
                FirstName = firstName,
                LastName = lastName,
                CompanyName = isCompany ? (!string.IsNullOrWhiteSpace(companyName) ? companyName.Trim() : $"{firstName} {lastName} للشحن") : null,
                UserType = isCompany ? UserType.DeliveryCompanyUser : UserType.DeliveryDriver,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                if (isCompany)
                {
                    await _userManager.AddToRoleAsync(user, "DeliveryCompanyUser");

                    // Save 3PL Company KYC documents
                    string? commDocUrl = await SaveKycFileAsync(commercialRegisterDoc, user.Id, "company_cr");
                    string? ltrcDocUrl = await SaveKycFileAsync(transportLicenseDoc, user.Id, "company_ltrc");

                    try
                    {
                        await _serviceManager.DeliveryService.CreateCompanyAsync(
                            name: user.CompanyName ?? $"{fullName} لخدمات الشحن واللوجستيات",
                            email: email.Trim(),
                            phoneNumber: phone?.Trim(),
                            commercialRegister: commercialRegister?.Trim() ?? "200189422",
                            baseRate: 25.00m,
                            managerUserId: user.Id.ToString(),
                            taxId: taxId?.Trim(),
                            transportCommissionLicense: transportCommissionLicense?.Trim(),
                            commercialRegisterDocUrl: commDocUrl,
                            transportLicenseDocUrl: ltrcDocUrl,
                            cliqAlias: companyCliqAlias?.Trim(),
                            totalTrucksCount: totalTrucksCount.HasValue && totalTrucksCount.Value > 0 ? totalTrucksCount.Value : 5,
                            isApproved: false
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Company creation error: " + ex.Message);
                    }
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "DeliveryDriver");

                    // Save Freelance Captain KYC documents
                    string? licDocUrl = await SaveKycFileAsync(licenseDoc, user.Id, "license");
                    string? regDocUrl = await SaveKycFileAsync(registrationDoc, user.Id, "registration");
                    string? vehPhotoUrl = await SaveKycFileAsync(vehiclePhoto, user.Id, "vehicle");

                    try
                    {
                        await _serviceManager.DeliveryService.RegisterDriverAsync(
                            userId: user.Id,
                            companyId: null,
                            vehicleType: vehicleType ?? "تريلا مقطورات مسطحة ثقيلة (30-40 طن)",
                            vehiclePlateNumber: plateNumber?.Trim() ?? "12-38491",
                            licenseNumber: "DL-" + user.Id,
                            nationalId: nationalId?.Trim(),
                            heavyLicenseCategory: heavyLicenseCategory?.Trim() ?? "الفئة السادسة - قاطرة ومقطورة",
                            licenseDocUrl: licDocUrl,
                            registrationDocUrl: regDocUrl,
                            vehiclePhotoUrl: vehPhotoUrl,
                            cliqAlias: driverCliqAlias?.Trim(),
                            capacityTons: capacityTons.HasValue && capacityTons.Value > 0 ? capacityTons.Value : 15,
                            coveredGovernorate: coveredGovernorate?.Trim() ?? "كافة محافظات المملكة"
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Driver registration error: " + ex.Message);
                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
                TempData["Success"] = isCompany 
                    ? "تم استلام طلب اعتماد شركة الشحن والأسطول بنجاح. سيتم مراجعة التراخيص والمصادقة على الحساب."
                    : "تم استلام طلب تسجيل كابتن النقل الثقيل بنجاح. ملفك قيد التدقيق والمطابقة لدى الإدارة.";

                return RedirectToAction("PendingApproval", "Home", new { area = "Delivery" });
            }

            ViewBag.Error = result.Errors.FirstOrDefault()?.Description ?? "فشل تسجيل حساب الناقل.";
            return View();
        }

        private async Task<string?> SaveKycFileAsync(IFormFile? file, int userId, string prefix)
        {
            if (file == null || file.Length == 0) return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) return null;

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "delivery", userId.ToString());
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = $"{prefix}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/delivery/{userId}/{fileName}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
