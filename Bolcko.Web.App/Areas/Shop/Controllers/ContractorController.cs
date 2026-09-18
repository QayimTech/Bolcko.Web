using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.User.DTOs;
using Bolcko.Domain.Enums;
using Bolcko.Web.App.Areas.Shop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IWebHostEnvironment _env;

        public ContractorController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IServiceManager serviceManager,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _serviceManager = serviceManager;
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
            if (int.TryParse(userIdStr, out var uId))
            {
                user = await _userManager.FindByIdAsync(uId.ToString());
            }

            ViewBag.IsNewRegistration = registered;
            ViewBag.User = user;

            // Stats from service manager
            var activeFinancings = await _serviceManager.FinancingService.GetContractorDashboardAsync(user?.Id, user?.PhoneNumber);
            ViewBag.FinancingDashboard = activeFinancings;

            return View(user);
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
