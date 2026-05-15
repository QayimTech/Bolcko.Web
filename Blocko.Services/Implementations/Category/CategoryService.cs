using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            return categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory?.Name,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        public async Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetRootCategoriesAsync();
            return categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId)
        {
            var categories = await _unitOfWork.Categories.FindAsync(c => c.ParentCategoryId == parentId);
            return categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.Name,
                DisplayOrder = category.DisplayOrder,
                ProductCount = category.Products?.Count ?? 0
            };
        }

        public async Task AddCategoryAsync(CategoryDto categoryDto)
        {
            var category = new Bolcko.Domain.Entities.Catalog.Category
            {
                Name = categoryDto.Name,
                Description = categoryDto.Description,
                ParentCategoryId = categoryDto.ParentCategoryId,
                DisplayOrder = categoryDto.DisplayOrder
            };
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateCategoryAsync(CategoryDto categoryDto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryDto.Id);
            if (category != null)
            {
                category.Name = categoryDto.Name;
                category.Description = categoryDto.Description;
                category.ParentCategoryId = categoryDto.ParentCategoryId;
                category.DisplayOrder = categoryDto.DisplayOrder;
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category != null)
            {
                _unitOfWork.Categories.Remove(category);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}