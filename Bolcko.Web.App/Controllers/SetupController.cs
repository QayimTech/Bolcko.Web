using Blocko.Persistence;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Controllers
{
    /// <summary>
    /// First-run setup controller. Accessible ONLY when no Admin user exists in the DB.
    /// Once an admin is created this controller redirects all traffic away.
    /// </summary>
    public class SetupController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly BlockoDbContext _db;

        public SetupController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            BlockoDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        // ── Guard helper ─────────────────────────────────────────────────────
        private async Task<bool> AdminExistsAsync()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            return admins.Count > 0;
        }

        // GET /Setup
        public async Task<IActionResult> Index()
        {
            if (await AdminExistsAsync())
                return RedirectToAction("Login", "Account", new { area = "Admin" });

            return View();
        }

        // POST /Setup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SetupViewModel model)
        {
            // Double-check: if admin already created, abort
            if (await AdminExistsAsync())
                return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (!ModelState.IsValid)
                return View(model);

            // 1. Ensure all roles exist
            string[] roles = { "Admin", "DashboardUser", "Customer" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole<int> { Name = role });
            }

            // 2. Create the super admin
            var admin = new User
            {
                UserName      = model.Email,
                Email         = model.Email,
                FirstName     = "Super",
                LastName      = "Admin",
                UserType      = UserType.Admin,
                EmailConfirmed = true,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(admin, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(admin, "Admin");

            // 3. Seed market prices if not already seeded
            if (!_db.MarketPrices.Any())
            {
                _db.MarketPrices.AddRange(
                    new Bolcko.Domain.Entities.Catalog.MarketPrice
                        { MaterialName = "حديد تسليح سابك", Price = 610.50m, Currency = "د.أ", UnitOfMeasure = "طن",        LastUpdated = DateTime.UtcNow },
                    new Bolcko.Domain.Entities.Catalog.MarketPrice
                        { MaterialName = "أسمنت الراجحي",   Price = 75.00m,  Currency = "د.أ", UnitOfMeasure = "طن",        LastUpdated = DateTime.UtcNow },
                    new Bolcko.Domain.Entities.Catalog.MarketPrice
                        { MaterialName = "خرسانة جاهزة",    Price = 45.00m,  Currency = "د.أ", UnitOfMeasure = "متر مكعب", LastUpdated = DateTime.UtcNow }
                );
                await _db.SaveChangesAsync();
            }

            TempData["SetupSuccess"] = "true";
            return RedirectToAction("Done");
        }

        // GET /Setup/Done
        public async Task<IActionResult> Done()
        {
            // If no admin yet somehow, go back to setup
            if (!await AdminExistsAsync())
                return RedirectToAction("Index");

            return View();
        }
    }

    // ── ViewModel ──────────────────────────────────────────────────────────
    public class SetupViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [System.ComponentModel.DataAnnotations.MinLength(12, ErrorMessage = "كلمة المرور يجب أن تكون 12 حرف على الأقل")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
