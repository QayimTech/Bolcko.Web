using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Order.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Models.ViewModels
{
    public class OrderIndexViewModel
    {
        public IPagedList<OrderDto> Orders { get; set; } = null!;
        public string? Search { get; set; }
        public Bolcko.Domain.Enums.OrderStatus? Status { get; set; }
        public string? SortOrder { get; set; }
    }
}
