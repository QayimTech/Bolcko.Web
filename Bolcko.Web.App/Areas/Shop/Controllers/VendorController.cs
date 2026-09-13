using Bolcko.Domain.Entities.Catalog.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class VendorController : Controller
    {
        // GET: /Shop/Vendor/Join -> Redirects to /Vendor/Join
        [HttpGet]
        public IActionResult Join()
        {
            return RedirectToActionPermanent("Join", "Account", new { area = "Vendor" });
        }

        // POST: /Shop/Vendor/Register -> Redirects to /Vendor/Join
        [HttpPost]
        public IActionResult Register(VendorRegistrationDto model)
        {
            return RedirectToActionPermanent("Join", "Account", new { area = "Vendor" });
        }

        // GET: /Shop/Vendor/Confirmation -> Redirects to /Vendor/Confirmation
        [HttpGet]
        public IActionResult Confirmation()
        {
            return RedirectToActionPermanent("Confirmation", "Account", new { area = "Vendor" });
        }

        // GET: /Shop/Vendor/Dashboard -> Redirects to /Vendor/Dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToActionPermanent("Index", "Dashboard", new { area = "Vendor" });
        }
    }
}
