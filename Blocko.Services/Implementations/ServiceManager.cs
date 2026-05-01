using Blocko.Services.Interfaces;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IUserService> _lazyUserService;
        private readonly Lazy<IProductService> _lazyProductService;
        private readonly Lazy<ICategoryService> _lazyCategoryService;
        private readonly Lazy<IOrderService> _lazyOrderService;
        private readonly Lazy<ITenderService> _lazyTenderService;

        public ServiceManager(IUnitOfWork unitOfWork)
        {
            _lazyUserService = new Lazy<IUserService>(() => new UserService(unitOfWork));
            _lazyProductService = new Lazy<IProductService>(() => new ProductService(unitOfWork));
            _lazyCategoryService = new Lazy<ICategoryService>(() => new CategoryService(unitOfWork));
            _lazyOrderService = new Lazy<IOrderService>(() => new OrderService(unitOfWork));
            _lazyTenderService = new Lazy<ITenderService>(() => new TenderService(unitOfWork));
        }

        public IUserService UserService => _lazyUserService.Value;
        public IProductService ProductService => _lazyProductService.Value;
        public ICategoryService CategoryService => _lazyCategoryService.Value;
        public IOrderService OrderService => _lazyOrderService.Value;
        public ITenderService TenderService => _lazyTenderService.Value;
    }
}