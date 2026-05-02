namespace Blocko.Services.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetSubCategoriesAsync(int parentId);
        Task<Bolcko.Domain.Entities.Catalog.Category?> GetCategoryByIdAsync(int id);
        Task AddCategoryAsync(Bolcko.Domain.Entities.Catalog.Category category);
        Task UpdateCategoryAsync(Bolcko.Domain.Entities.Catalog.Category category);
        Task DeleteCategoryAsync(int id);
    }
}