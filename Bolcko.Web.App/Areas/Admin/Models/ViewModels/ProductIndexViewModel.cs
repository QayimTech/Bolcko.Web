using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Product.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Models.ViewModels
{
    public class ProductIndexViewModel
    {
        public IPagedList<ProductDto> Products { get; set; } = null!;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? SortOrder { get; set; }
    }
}
