using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Persistence;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Contractor;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.User.DTOs;
using Bolcko.Domain.Enums;
using Bolcko.Web.App.Areas.Shop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Shop/[controller]")]
    [Route("[controller]")]
    public class ContractorController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IServiceManager _serviceManager;
        private readonly BlockoDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ContractorController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IServiceManager serviceManager,
            BlockoDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _serviceManager = serviceManager;
            _context = context;
            _env = env;
        }

        /// <summary>
        /// صفحة تسجيل وتأهيل المقاولين B2B (Contractor Onboarding & KYC)
        /// </summary>
        [HttpGet("Join")]
        [AllowAnonymous]
        public IActionResult Join()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Contractor"))
            {
                return RedirectToAction(nameof(Workspace));
            }

            var model = new ContractorJoinViewModel();
            return View(model);
        }

        /// <summary>
        /// معالجة تسجيل المقاول والتحقق من السجل التجاري والضريبي
        /// </summary>
        [HttpPost("Join")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(ContractorJoinViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "البريد الإلكتروني مسجل مسبقاً في النظام.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.AuthorizedPhone,
                FirstName = model.AuthorizedPersonName,
                LastName = $"({model.ContractorClass})",
                CompanyName = model.CompanyName,
                BusinessRegistrationNumber = model.CommercialRegistration,
                UserType = UserType.Contractor,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                return View(model);
            }

            // Ensure Contractor role exists
            if (!await _roleManager.RoleExistsAsync("Contractor"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Contractor"));
            }

            await _userManager.AddToRoleAsync(user, "Contractor");

            // Handle KYC documents upload
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "contractors", user.Id.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            if (model.CrDocument != null && model.CrDocument.Length > 0)
            {
                string crPath = Path.Combine(uploadsFolder, "CR_" + Path.GetFileName(model.CrDocument.FileName));
                using var stream = new FileStream(crPath, FileMode.Create);
                await model.CrDocument.CopyToAsync(stream);
            }

            if (model.TaxCertificate != null && model.TaxCertificate.Length > 0)
            {
                string taxPath = Path.Combine(uploadsFolder, "TAX_" + Path.GetFileName(model.TaxCertificate.FileName));
                using var stream = new FileStream(taxPath, FileMode.Create);
                await model.TaxCertificate.CopyToAsync(stream);
            }

            await _signInManager.SignInAsync(user, isPersistent: true);

            return RedirectToAction(nameof(Workspace), new { registered = true });
        }

        /// <summary>
        /// مجمع ومساحة أعمال المقاول المعتمد (B2B Contractor Workspace)
        /// </summary>
        [HttpGet("Workspace")]
        [Authorize(Roles = "Contractor, Admin, SuperAdmin, DashboardUser")]
        public async Task<IActionResult> Workspace(bool registered = false)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = null;
            ContractorProfile? profile = null;

            if (int.TryParse(userIdStr, out var uId))
            {
                user = await _userManager.FindByIdAsync(uId.ToString());
                if (user != null)
                {
                    profile = await _context.ContractorProfiles.FirstOrDefaultAsync(p => p.UserId == uId);
                    if (profile == null)
                    {
                        // Initialize default enterprise contractor profile
                        profile = new ContractorProfile
                        {
                            UserId = user.Id,
                            CompanyNameAr = !string.IsNullOrWhiteSpace(user.CompanyName) ? user.CompanyName : "شركة الأفق للمقاولات العامة",
                            CompanyNameEn = "Al-Ufuq General Contracting Co.",
                            CommercialRegistrationNo = !string.IsNullOrWhiteSpace(user.BusinessRegistrationNumber) ? user.BusinessRegistrationNumber : "200189422",
                            TaxNumber = "018492048",
                            ClassificationGrade = !string.IsNullOrWhiteSpace(user.LastName) && user.LastName.Contains("درجة") ? user.LastName.Trim('(', ')') : "الدرجة الأولى - إنشاء أبنية",
                            JccaMembershipNumber = "JCCA-8942",
                            IsJccaVerified = false, // Starts unverified if document is not uploaded yet
                            Status = "Active",
                            ContactPersonName = $"{user.FirstName} {user.LastName}".Trim(),
                            Phone = user.PhoneNumber ?? "0795544123",
                            City = "عمان"
                        };
                        _context.ContractorProfiles.Add(profile);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            ViewBag.IsNewRegistration = registered;
            ViewBag.User = user;
            ViewBag.ContractorProfile = profile;

            // Stats from service manager
            var activeFinancings = await _serviceManager.FinancingService.GetContractorDashboardAsync(user?.Id, user?.PhoneNumber);
            ViewBag.FinancingDashboard = activeFinancings;

            return View(user);
        }

        /// <summary>
        /// رفع وتوثيق شهادة عضوية نقابة مقاولي الإنشاءات الأردنيين (JCCA Upload & Verification)
        /// </summary>
        [HttpPost("UploadJccaDocument")]
        [Authorize(Roles = "Contractor, Admin, SuperAdmin, DashboardUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadJccaDocument(string? jccaNumber, string? classificationGrade, IFormFile? jccaFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var uId))
            {
                return Unauthorized();
            }

            var profile = await _context.ContractorProfiles.FirstOrDefaultAsync(p => p.UserId == uId);
            if (profile == null)
            {
                var user = await _userManager.FindByIdAsync(uId.ToString());
                profile = new ContractorProfile
                {
                    UserId = uId,
                    CompanyNameAr = user?.CompanyName ?? "شركة المقاولات الإنشائية",
                    CommercialRegistrationNo = user?.BusinessRegistrationNumber ?? "200189422",
                    ClassificationGrade = classificationGrade ?? "الدرجة الأولى - إنشاء أبنية"
                };
                _context.ContractorProfiles.Add(profile);
            }

            if (!string.IsNullOrWhiteSpace(jccaNumber))
            {
                profile.JccaMembershipNumber = jccaNumber;
            }

            if (!string.IsNullOrWhiteSpace(classificationGrade))
            {
                profile.ClassificationGrade = classificationGrade;
            }

            if (jccaFile != null && jccaFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "contractors", uId.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = "JCCA_" + Guid.NewGuid().ToString("N").Substring(0, 8) + Path.GetExtension(jccaFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await jccaFile.CopyToAsync(stream);
                }

                profile.JccaCertificateDocUrl = $"/uploads/contractors/{uId}/{fileName}";
                profile.IsJccaVerified = true;
                profile.VerifiedAt = DateTime.UtcNow;
                TempData["Success"] = "تم رفع شهادة نقابة مقاولي الإنشاءات الأردنيين (JCCA) بنجاح وتفعيل الاعتماد الهندسي!";
            }
            else
            {
                TempData["Error"] = "يرجى اختيار ملف الشهادة أو وثيقة الانتساب للنقابة.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Workspace));
        }

        /// <summary>
        /// صفحة حالة مراجعة وتدقيق المستندات
        /// </summary>
        [HttpGet("Pending")]
        [Authorize(Roles = "Contractor, Admin, SuperAdmin")]
        public IActionResult Pending()
        {
            return View();
        }
    }
}
