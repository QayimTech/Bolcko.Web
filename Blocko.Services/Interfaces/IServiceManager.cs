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