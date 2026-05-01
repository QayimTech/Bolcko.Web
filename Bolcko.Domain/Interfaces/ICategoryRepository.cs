using Bolcko.Domain.Entities.Catalog;

namespace Bolcko.Domain.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category> 
    {
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
    }
}