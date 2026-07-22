using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class OrderController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public OrderController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, Bolcko.Domain.Enums.OrderStatus? status = null, string? sortOrder = null)
        {
            var orders = await _serviceManager.OrderService.GetPagedOrdersAsync(page, pageSize, search, status, sortOrder);
            var viewModel = new OrderIndexViewModel
            {
                Orders = orders,
                Search = search,
                Status = status,
                SortOrder = sortOrder
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _serviceManager.OrderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            var companiesPaged = await _serviceManager.DeliveryService.GetPagedCompaniesAsync(1, 100);
            ViewBag.DeliveryCompanies = companiesPaged?.Items ?? new List<Bolcko.Domain.Entities.Delivery.DeliveryCompany>();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, Bolcko.Domain.Enums.OrderStatus status)
        {
            var result = await _serviceManager.OrderService.UpdateOrderStatusAsync(id, status);
            if (!result) return NotFound();

            TempData["SuccessMessage"] = "تم تحديث حالة الطلب بنجاح";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
