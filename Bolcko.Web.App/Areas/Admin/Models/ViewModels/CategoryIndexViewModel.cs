using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Models.ViewModels
{
    public class CategoryIndexViewModel
    {
        public IPagedList<CategoryDto> Categories { get; set; } = null!;
    }
}
