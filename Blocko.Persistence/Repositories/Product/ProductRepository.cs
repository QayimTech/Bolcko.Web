using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Product
{
    public class ProductRepository : GenericRepository<Bolcko.Domain.Entities.Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product>> GetProductsByCategoryAsync(int categoryId) => 
            await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product>> GetFeaturedProductsAsync() => 
            await _context.Products.Take(10).ToListAsync();
    }
}