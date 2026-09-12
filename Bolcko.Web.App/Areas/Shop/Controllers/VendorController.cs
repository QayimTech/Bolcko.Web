using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class VendorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IServiceManager _serviceManager;

        public VendorController(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IServiceManager serviceManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _serviceManager = serviceManager;
        }

        // GET: /Vendor/Join
        [HttpGet]
        public IActionResult Join()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // If already logged in, check if they are already a vendor
                var userTypeClaim = User.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;
                if (userTypeClaim == UserType.Vendor.ToString())
                {
                    return RedirectToAction("Dashboard");
                }
            }

            var model = new VendorRegistrationDto
            {
                CountryCode = "JO",
                City = "Amman"
            };

            return View(model);
        }

        // POST: /Vendor/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VendorRegistrationDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("Join", model);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "البريد الإلكتروني مسجل مسبقاً، يرجى تسجيل الدخول أو استخدام بريد آخر.");
                return View("Join", model);
            }

            // Split name into first and last
            var nameParts = (model.ContactPersonName ?? "").Trim().Split(' ');
            var firstName = nameParts.Length > 0 ? nameParts[0] : "مورد";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "معتمد";

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                FirstName = firstName,
                LastName = lastName,
                CompanyName = model.CompanyNameAr,
                BusinessRegistrationNumber = model.CommercialRegistrationNo,
                UserType = UserType.Vendor,
                RegistrationDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                return View("Join", model);
            }

            // Create VendorProfile
            var vendorProfile = new VendorProfile
            {
                UserId = user.Id,
                CompanyNameAr = model.CompanyNameAr,
                CompanyNameEn = model.CompanyNameEn ?? model.CompanyNameAr,
                CommercialRegistrationNo = model.CommercialRegistrationNo,
                TaxNumber = model.TaxNumber ?? string.Empty,
                CountryCode = model.CountryCode,
                City = model.City,
                AddressText = model.AddressText ?? string.Empty,
                Phone = model.Phone,
                WhatsApp = model.WhatsApp,
                ContactPersonName = model.ContactPersonName,
                SuppliedCategories = string.Join(",", model.SuppliedCategories ?? new System.Collections.Generic.List<string>()),
                Status = "PendingVerification", // Needs admin verification
                RegisteredAt = DateTime.UtcNow
            };

            await _unitOfWork.VendorProfiles.AddAsync(vendorProfile);
            await _unitOfWork.CompleteAsync();

            // Sign in vendor
            await _signInManager.SignInAsync(user, isPersistent: true);

            TempData["SuccessMessage"] = "أهلاً بك في شبكة موردين بلوكو! تم تسجيل حسابك بنجاح وهو قيد المراجعة والاعتماد.";
            return RedirectToAction("Confirmation");
        }

        // GET: /Vendor/Confirmation
        [HttpGet]
        public IActionResult Confirmation()
        {
            return View();
        }

        // GET: /Vendor/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Join");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Join");
            }

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            return View(profile);
        }
    }
}
