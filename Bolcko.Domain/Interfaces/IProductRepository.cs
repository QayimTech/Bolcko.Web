using Bolcko.Domain.Entities.Product;

namespace Bolcko.Domain.Interfaces
{
    public interface IProductRepository : IGenericRepository<Bolcko.Domain.Entities.Product.Product> 
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetFeaturedProductsAsync();
        Task<Bolcko.Domain.Entities.Product.Product?> GetByIdWithImagesAsync(int id);
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> SearchProductsAsync(string? query);
    }
}