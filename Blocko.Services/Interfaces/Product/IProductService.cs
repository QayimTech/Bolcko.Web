using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Common;
using Blocko.Services.Interfaces;

namespace Blocko.Services.Interfaces.Product
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IPagedList<ProductDto>> GetPagedProductsAsync(int pageIndex, int pageSize, string? search = null, int? categoryId = null, string? sortOrder = null);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync();
        Task<IEnumerable<ProductDto>> SearchProductsAsync(string? query);
        Task<IPagedList<ProductDto>> SearchCatalogProductsPagedAsync(string? query, int? categoryId, int pageIndex, int pageSize);
        Task AddProductAsync(ProductDto productDto);
        Task UpdateProductAsync(ProductDto productDto, List<int>? deleteImageIds = null);
        Task DeleteProductAsync(int id);
        Task<(int translated, int skipped, int failed)> BulkTranslateAsync(ITranslationService translationService);
    }
}