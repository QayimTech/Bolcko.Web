using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetRootCategoriesAsync() => await _unitOfWork.Categories.GetRootCategoriesAsync();
        public async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetSubCategoriesAsync(int parentId) => await _unitOfWork.Categories.FindAsync(c => c.ParentCategoryId == parentId);
    }
}