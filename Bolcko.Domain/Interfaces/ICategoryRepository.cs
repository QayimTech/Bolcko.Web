using Bolcko.Domain.Entities.Catalog;

namespace Bolcko.Domain.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category> 
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesWithProductsAsync(int parentId);
        Task<Category?> GetCategoryWithParentAsync(int id);
    }
}