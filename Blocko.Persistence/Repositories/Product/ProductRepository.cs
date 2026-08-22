using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Product
{
    public class ProductRepository : GenericRepository<Bolcko.Domain.Entities.Product.Product>, IProductRepository
    {
        public ProductRepository(BlockoDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetProductsByCategoryAsync(int categoryId) => 
            await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> GetFeaturedProductsAsync() => 
            await _context.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .OrderByDescending(p => p.Id) // Simplify ordering to avoid heavy subquery joins and aggregate calculations on every page load
                .Take(10)
                .ToListAsync();

        public async Task<Bolcko.Domain.Entities.Product.Product?> GetByIdWithImagesAsync(int id) =>
            await _context.Products.AsNoTracking().Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Bolcko.Domain.Entities.Product.Product?> GetByIdWithImagesAndVariantsAsync(int id) =>
            await _context.Products.Include(p => p.Images).Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Bolcko.Domain.Entities.Product.Product>> SearchProductsAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.Products.ToListAsync();
            }

            return await _context.Products.Where(p => 
                (p.Name != null && p.Name.Contains(query)) || 
                (p.Description != null && p.Description.Contains(query))
            ).ToListAsync();
        }
    }
}