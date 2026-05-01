using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> RegisterUserAsync(User user, string password);
        Task<User?> GetUserByIdAsync(int id);
    }

    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync();
    }

    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
    }

    public interface IOrderService
    {
        Task<Order> PlaceOrderAsync(Order order);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    }

    public interface ITenderService
    {
        Task<Tender> CreateTenderAsync(Tender tender);
        Task<IEnumerable<Tender>> GetOpenTendersAsync();
    }
}