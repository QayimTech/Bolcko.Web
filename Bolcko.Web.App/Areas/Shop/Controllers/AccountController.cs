using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class AccountController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public AccountController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _serviceManager.UserService.AuthenticateAsync(email, password);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.UserType.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                if (user.UserType == UserType.Admin)
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                
                return RedirectToAction("Index");
            }
            
            ViewBag.Error = "بيانات الدخول غير صحيحة";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Bolcko.Domain.Entities.User.User user, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "كلمات المرور غير متطابقة";
                return View(user);
            }

            var existingUser = await _serviceManager.UserService.AuthenticateAsync(user.Email, password);
            if (existingUser != null)
            {
                ViewBag.Error = "البريد الإلكتروني مستخدم بالفعل";
                return View(user);
            }

            await _serviceManager.UserService.RegisterUserAsync(user, password);
            
            // Auto login after registration
            return await Login(user.Email, password);
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim.Value);
            var user = await _serviceManager.UserService.GetUserByIdAsync(userId); 
            
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

        public IActionResult Orders()
        {
            return View();
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