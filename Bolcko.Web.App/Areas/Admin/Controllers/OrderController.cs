using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public OrderController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var orders = await _serviceManager.OrderService.GetPagedOrdersAsync(page, pageSize);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _serviceManager.OrderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
