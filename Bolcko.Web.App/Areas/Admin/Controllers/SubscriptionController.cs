using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,DashboardUser")]
    [Route("Admin/[controller]")]
    public class SubscriptionController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public SubscriptionController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? role = null)
        {
            var plans = await _serviceManager.SubscriptionService.GetPlansAsync(role);
            var stats = await _serviceManager.SubscriptionService.GetSubscriptionStatsAsync();
            var subscribers = await _serviceManager.SubscriptionService.GetAllSubscribersAsync(role);

            ViewBag.Stats = stats;
            ViewBag.Subscribers = subscribers;
            ViewBag.SelectedRole = role;

            return View(plans);
        }
    }
}
