using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Common;

namespace Blocko.Services.Interfaces.Product
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IPagedList<ProductDto>> GetPagedProductsAsync(int pageIndex, int pageSize);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync();
        Task AddProductAsync(ProductDto productDto);
        Task UpdateProductAsync(ProductDto productDto, List<int>? deleteImageIds = null);
        Task DeleteProductAsync(int id);
    }
}