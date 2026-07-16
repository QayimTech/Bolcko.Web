using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Category
{
    public class CategoryRepository : GenericRepository<Bolcko.Domain.Entities.Catalog.Category>, ICategoryRepository
    {
        public CategoryRepository(BlockoDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetRootCategoriesAsync() => 
            await _context.Categories
                .Include(c => c.Products)
                .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products)
                .Where(c => c.ParentCategoryId == null)
                .OrderByDescending(c => c.Products.Count())
                .Take(10)
                .ToListAsync();

        public async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetSubCategoriesWithProductsAsync(int parentId) =>
            await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.ParentCategoryId == parentId)
                .ToListAsync();

        public async Task<Bolcko.Domain.Entities.Catalog.Category?> GetCategoryWithParentAsync(int id) =>
            await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);
    }
}