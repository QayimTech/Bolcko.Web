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
            string? vehicleType,
            string? plateNumber,
            int? capacityTons,
            string? coveredCity,
            string? registrationType = null,
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

            bool isCompany = !string.IsNullOrWhiteSpace(companyName) || registrationType == "company";

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
                    // Persist DeliveryCompany record
                    try
                    {
                        await _serviceManager.DeliveryService.CreateCompanyAsync(
                            user.CompanyName ?? $"{fullName} لخدمات الشحن",
                            email.Trim(),
                            phone?.Trim(),
                            "200189422",
                            25.00m,
                            user.Id.ToString()
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
                    // Persist DeliveryDriver record
                    try
                    {
                        await _serviceManager.DeliveryService.RegisterDriverAsync(
                            user.Id,
                            null,
                            vehicleType ?? "ونش تفريغ هيدروليكي",
                            plateNumber ?? "12-38491",
                            "DL-" + user.Id
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Driver registration error: " + ex.Message);
                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "Delivery" });
            }

            ViewBag.Error = result.Errors.FirstOrDefault()?.Description ?? "فشل تسجيل حساب الناقل.";
            return View();
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
