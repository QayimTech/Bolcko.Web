using Bolcko.Domain.Entities;

namespace Bolcko.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User> 
    {
        Task<User?> GetByEmailAsync(string email);
    }

    public interface IProductRepository : IGenericRepository<Product> 
    {
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync();
    }

    public interface ICategoryRepository : IGenericRepository<Category> 
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
    }

    public interface IOrderRepository : IGenericRepository<Order> 
    {
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    }

    public interface ITenderRepository : IGenericRepository<Tender> 
    {
        Task<IEnumerable<Tender>> GetUserTendersAsync(int userId);
        Task<IEnumerable<Tender>> GetOpenTendersAsync();
    }

    public interface IProjectRepository : IGenericRepository<Project> 
    {
        Task<IEnumerable<Project>> GetUserProjectsAsync(int userId);
    }

    public interface IMarketPriceRepository : IGenericRepository<MarketPrice> 
    {
        Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName);
    }
}