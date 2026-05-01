using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Category
{
    public class CategoryRepository : GenericRepository<Bolcko.Domain.Entities.Catalog.Category>, ICategoryRepository
    {
        public CategoryRepository(BlockoDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.Category>> GetRootCategoriesAsync() => 
            await _context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
    }
}