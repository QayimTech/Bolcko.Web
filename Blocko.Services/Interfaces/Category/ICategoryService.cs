using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Common;
using Blocko.Services.Interfaces;

namespace Blocko.Services.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IPagedList<CategoryDto>> GetPagedCategoriesAsync(int pageIndex, int pageSize, string? search = null);
        Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId);
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task AddCategoryAsync(CategoryDto categoryDto);
        Task UpdateCategoryAsync(CategoryDto categoryDto);
        Task DeleteCategoryAsync(int id);
        Task<(int translated, int skipped, int failed)> BulkTranslateCategoriesAsync(ITranslationService translationService);
    }
}
