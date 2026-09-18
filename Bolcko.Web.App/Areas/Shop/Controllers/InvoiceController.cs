using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Tax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Shop/[controller]")]
    [Route("[controller]")]
    public class InvoiceController : Controller
    {
        private readonly ITaxInvoicingService _taxInvoicingService;

        public InvoiceController(ITaxInvoicingService taxInvoicingService)
        {
            _taxInvoicingService = taxInvoicingService;
        }

        /// <summary>
        /// عرض وطباعة الفاتورة الضريبية (B2C مبسطة أو B2B قياسية للشركات)
        /// </summary>
        [HttpGet("Print/{orderId}")]
        [Authorize]
        public async Task<IActionResult> Print(int orderId)
        {
            var userRole = User.IsInRole("Contractor") ? "Contractor" : (User.IsInRole("Vendor") ? "Vendor" : "Customer");
            var invoice = await _taxInvoicingService.GenerateInvoiceForOrderAsync(orderId, userRole);
            return View(invoice);
        }
    }
}
