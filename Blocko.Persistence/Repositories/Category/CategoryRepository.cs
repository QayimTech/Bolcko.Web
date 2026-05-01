using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Category
{
    public class CategoryRepository : GenericRepository<Bolcko.Domain.Entities.Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Category>> GetRootCategoriesAsync() => 
            await _context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
    }
}