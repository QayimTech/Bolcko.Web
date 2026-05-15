using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.User;
using Blocko.Services.Interfaces.SEO;
using Blocko.Services.Interfaces.ShoppingCart;

namespace Blocko.Services.Interfaces
{
    public interface IServiceManager
    {
        IUserService UserService { get; }
        IProductService ProductService { get; }
        ICategoryService CategoryService { get; }
        IMarketPriceService MarketPriceService { get; }
        IOrderService OrderService { get; }
        ITenderService TenderService { get; }
        ISEOService SEOService { get; }
        IShoppingCartService ShoppingCartService { get; }
    }
}