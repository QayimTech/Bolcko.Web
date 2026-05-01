using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Bolcko.Domain.Entities.Category>> GetSubCategoriesAsync(int parentId);
    }
}