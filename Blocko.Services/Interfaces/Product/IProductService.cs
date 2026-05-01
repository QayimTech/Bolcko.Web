namespace Blocko.Services.Interfaces.Product
{
    public interface IProductService
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetAllProductsAsync();
        Task<Bolcko.Domain.Entities.Product.Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetFeaturedProductsAsync();
    }
}