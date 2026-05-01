using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.User;

namespace Blocko.Services.Interfaces
{
    public interface IServiceManager
    {
        IUserService UserService { get; }
        IProductService ProductService { get; }
        ICategoryService CategoryService { get; }
        IOrderService OrderService { get; }
        ITenderService TenderService { get; }
    }
}