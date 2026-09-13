using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public DashboardController(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: /Vendor/Dashboard or /Vendor/Dashboard/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Vendor" });
            }

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            // If user has no vendor profile yet, redirect them to join or provide fallback
            return View(profile);
        }
    }
}
