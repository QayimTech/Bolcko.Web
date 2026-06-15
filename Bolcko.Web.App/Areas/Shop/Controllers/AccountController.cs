using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class AccountController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(IServiceManager serviceManager, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Redirect by role (not by UserType) to keep authorization consistent
                    if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Index", "Home", new { area = "Admin" });

                    return RedirectToAction("Index");
                }
            }
            
            ViewBag.Error = "بيانات الدخول غير صحيحة";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "كلمات المرور غير متطابقة";
                return View(user);
            }

            user.UserName = user.Email;
            user.UserType = UserType.Customer;
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Default role for new users
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: true);
                
                return RedirectToAction("Index");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View(user);
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var orders = await _serviceManager.OrderService.GetUserOrdersAsync(user.Id);
            var tenders = await _serviceManager.TenderService.GetOpenTendersAsync(); 
            
            ViewBag.User = user;
            ViewBag.Orders = orders;
            ViewBag.Tenders = tenders;

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var orders = await _serviceManager.OrderService.GetUserOrdersAsync(user.Id);
            ViewBag.User = user;
            
            return View(orders);
        }

        public IActionResult Quotes()
        {
            return View();
        }

        public IActionResult Projects()
        {
            return View();
        }
    }
}
