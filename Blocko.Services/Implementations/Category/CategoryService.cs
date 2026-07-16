using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Common;
using Blocko.Persistence.Common;

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
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        public async Task<IPagedList<CategoryDto>> GetPagedCategoriesAsync(int pageIndex, int pageSize)
        {
            var pagedCategories = await _unitOfWork.Categories.GetPagedAsync(
                pageIndex,
                pageSize,
                orderBy: q => q.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name),
                includes: c => c.Products!
            );

            var dtos = pagedCategories.Items.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory?.Name,
                DisplayOrder = c.DisplayOrder,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products?.Count ?? 0
            });

            return new PagedList<CategoryDto>(dtos, pagedCategories.TotalCount, pageIndex, pageSize);
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
                ImageUrl = c.ImageUrl,
                ProductCount = (c.Products?.Count ?? 0) + (c.SubCategories?.Sum(sc => sc.Products?.Count ?? 0) ?? 0)
            });
        }

        public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId)
        {
            var categories = await _unitOfWork.Categories.GetSubCategoriesWithProductsAsync(parentId);
            return categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetCategoryWithParentAsync(id);
            if (category == null) return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.Name,
                DisplayOrder = category.DisplayOrder,
                ImageUrl = category.ImageUrl,
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
                DisplayOrder = categoryDto.DisplayOrder,
                ImageUrl = categoryDto.ImageUrl
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
                category.ImageUrl = categoryDto.ImageUrl;
                _unitOfWork.Categories.Update(category);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category != null)
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await DeleteCategoryRecursiveAsync(category);
                    await _unitOfWork.CompleteAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
        }

        private async Task DeleteCategoryRecursiveAsync(Bolcko.Domain.Entities.Catalog.Category category)
        {
            // 1. Recursively delete subcategories first
            var subCategories = await _unitOfWork.Categories.FindAsync(c => c.ParentCategoryId == category.Id);
            foreach (var sub in subCategories)
            {
                await DeleteCategoryRecursiveAsync(sub);
            }

            // 2. Get all products in this category
            var products = await _unitOfWork.Products.FindAsync(p => p.CategoryId == category.Id);
            foreach (var product in products)
            {
                // 2a. Delete OrderItems referencing this product
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.ProductId == product.Id);
                foreach (var oi in orderItems)
                {
                    _unitOfWork.OrderItems.Remove(oi);
                }

                // 2b. Delete ShoppingCartItems referencing this product
                var cartItems = await _unitOfWork.ShoppingCartItems.FindAsync(ci => ci.ProductId == product.Id);
                foreach (var ci in cartItems)
                {
                    _unitOfWork.ShoppingCartItems.Remove(ci);
                }

                // 2c. Delete ProductImages (should cascade, but explicit for safety)
                var images = await _unitOfWork.ProductImages.FindAsync(img => img.ProductId == product.Id);
                foreach (var img in images)
                {
                    _unitOfWork.ProductImages.Remove(img);
                }

                // 2d. Delete the product itself
                _unitOfWork.Products.Remove(product);
            }

            // 3. Finally remove the category
            _unitOfWork.Categories.Remove(category);
        }
    }
}