using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces.Product
{
    public interface IProductService
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Product>> GetAllProductsAsync();
        Task<Bolcko.Domain.Entities.Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Bolcko.Domain.Entities.Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Bolcko.Domain.Entities.Product>> GetFeaturedProductsAsync();
    }
}