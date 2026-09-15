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
                return await _context.Products.Include(p => p.Images).Include(p => p.Category).ToListAsync();
            }

            var trimmed = query.Trim();
            var pattern = $"%{trimmed}%";
            var qLower = trimmed.ToLowerInvariant();

            // Detect synonym terms
            var isCement = qLower.Contains("cement") || trimmed.Contains("اسمنت") || trimmed.Contains("إسمنت");
            var isSteel = qLower.Contains("steel") || qLower.Contains("rebar") || trimmed.Contains("حديد");
            var isBlock = qLower.Contains("block") || trimmed.Contains("طوب") || trimmed.Contains("بلوك") || trimmed.Contains("طابوق");
            var isStone = qLower.Contains("stone") || trimmed.Contains("حجر");

            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => 
                    (p.Name != null && EF.Functions.ILike(p.Name, pattern)) || 
                    (p.NameEn != null && EF.Functions.ILike(p.NameEn, pattern)) ||
                    (p.Description != null && EF.Functions.ILike(p.Description, pattern)) ||
                    (p.DescriptionEn != null && EF.Functions.ILike(p.DescriptionEn, pattern)) ||
                    (p.Sku != null && EF.Functions.ILike(p.Sku, pattern)) ||
                    (p.Brand != null && EF.Functions.ILike(p.Brand, pattern)) ||
                    (isCement && (
                        (p.Name != null && (EF.Functions.ILike(p.Name, "%اسمنت%") || EF.Functions.ILike(p.Name, "%إسمنت%") || EF.Functions.ILike(p.Name, "%cement%"))) ||
                        (p.NameEn != null && EF.Functions.ILike(p.NameEn, "%cement%"))
                    )) ||
                    (isSteel && (
                        (p.Name != null && (EF.Functions.ILike(p.Name, "%حديد%") || EF.Functions.ILike(p.Name, "%steel%"))) ||
                        (p.NameEn != null && EF.Functions.ILike(p.NameEn, "%steel%"))
                    )) ||
                    (isBlock && (
                        (p.Name != null && (EF.Functions.ILike(p.Name, "%طوب%") || EF.Functions.ILike(p.Name, "%بلوك%") || EF.Functions.ILike(p.Name, "%block%"))) ||
                        (p.NameEn != null && EF.Functions.ILike(p.NameEn, "%block%"))
                    )) ||
                    (isStone && (
                        (p.Name != null && (EF.Functions.ILike(p.Name, "%حجر%") || EF.Functions.ILike(p.Name, "%stone%"))) ||
                        (p.NameEn != null && EF.Functions.ILike(p.NameEn, "%stone%"))
                    ))
                ).ToListAsync();
        }
    }
}