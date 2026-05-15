using Bolcko.Domain.Entities.Catalog.DTOs;
using Blocko.Services.Common;

namespace Blocko.Services.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IPagedList<CategoryDto>> GetPagedCategoriesAsync(int pageIndex, int pageSize);
        Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId);
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task AddCategoryAsync(CategoryDto categoryDto);
        Task UpdateCategoryAsync(CategoryDto categoryDto);
        Task DeleteCategoryAsync(int id);
    }
}
