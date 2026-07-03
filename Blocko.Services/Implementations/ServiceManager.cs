using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.User;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.ShoppingCart;
using Blocko.Services.Implementations.Product;
using Blocko.Services.Implementations.Category;
using Blocko.Services.Implementations.Tender;
using Blocko.Services.Implementations.shoppingCart;
using Bolcko.Domain.Interfaces;
using Blocko.Services.Implementations.user;

using Blocko.Services.Interfaces.SEO;
using Blocko.Services.Implementations.SEO;
using Blocko.Services.Implementations.order;

namespace Blocko.Services.Implementations
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IUserService> _lazyUserService;
        private readonly Lazy<IProductService> _lazyProductService;
        private readonly Lazy<ICategoryService> _lazyCategoryService;
        private readonly Lazy<IMarketPriceService> _lazyMarketPriceService;
        private readonly Lazy<IOrderService> _lazyOrderService;
        private readonly Lazy<ITenderService> _lazyTenderService;
        private readonly Lazy<ISEOService> _lazySEOService;
        private readonly Lazy<IShoppingCartService> _lazyShoppingCartService;
        private readonly Lazy<IProjectService> _lazyProjectService;

        public ServiceManager(IUnitOfWork unitOfWork, Blocko.Services.Interfaces.Notifications.INotificationService notificationService)
        {
            _lazyUserService = new Lazy<IUserService>(() => new UserService(unitOfWork));
            _lazyProductService = new Lazy<IProductService>(() => new ProductService(unitOfWork));
            _lazyCategoryService = new Lazy<ICategoryService>(() => new CategoryService(unitOfWork));
            _lazyMarketPriceService = new Lazy<IMarketPriceService>(() => new MarketPriceService(unitOfWork));
            _lazyOrderService = new Lazy<IOrderService>(() => new OrderService(unitOfWork, notificationService));
            _lazyTenderService = new Lazy<ITenderService>(() => new TenderService(unitOfWork));
            _lazySEOService = new Lazy<ISEOService>(() => new SEOService(unitOfWork));
            _lazyShoppingCartService = new Lazy<IShoppingCartService>(() => new ShoppingCartService(unitOfWork));
            _lazyProjectService = new Lazy<IProjectService>(() => new ProjectService(unitOfWork));
        }

        public IUserService UserService => _lazyUserService.Value;
        public IProductService ProductService => _lazyProductService.Value;
        public ICategoryService CategoryService => _lazyCategoryService.Value;
        public IMarketPriceService MarketPriceService => _lazyMarketPriceService.Value;
        public IOrderService OrderService => _lazyOrderService.Value;
        public ITenderService TenderService => _lazyTenderService.Value;
        public ISEOService SEOService => _lazySEOService.Value;
        public IShoppingCartService ShoppingCartService => _lazyShoppingCartService.Value;
        public IProjectService ProjectService => _lazyProjectService.Value;
    }
}