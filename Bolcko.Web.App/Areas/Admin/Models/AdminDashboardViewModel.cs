using System.Collections.Generic;
using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Entities.Tender.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int UserCount { get; set; }
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int OpenTenders { get; set; }
        public IEnumerable<OrderDto> LatestOrders { get; set; } = new List<OrderDto>();
        public IEnumerable<TenderDto> LatestTenders { get; set; } = new List<TenderDto>();
    }
}
