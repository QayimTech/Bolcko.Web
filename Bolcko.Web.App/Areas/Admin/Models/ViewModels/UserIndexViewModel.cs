using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Web.App.Areas.Admin.Models.ViewModels
{
    public class UserIndexViewModel
    {
        public IPagedList<User> Users { get; set; } = null!;
    }
}
