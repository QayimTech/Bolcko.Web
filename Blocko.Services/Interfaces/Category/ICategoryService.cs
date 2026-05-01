namespace Blocko.Services.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetSubCategoriesAsync(int parentId);
    }
}