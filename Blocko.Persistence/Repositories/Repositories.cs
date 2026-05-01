using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }
        public async Task<User?> GetByEmailAsync(string email) => 
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) { }
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId) => 
            await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();
        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync() => 
            await _context.Products.Take(10).ToListAsync(); // Example logic
    }

    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync() => 
            await _context.Categories.Where(c => c.ParentCategoryId == null).ToListAsync();
    }

    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }
        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId) => 
            await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
    }

    public class TenderRepository : GenericRepository<Tender>, ITenderRepository
    {
        public TenderRepository(ApplicationDbContext context) : base(context) { }
        public async Task<IEnumerable<Tender>> GetUserTendersAsync(int userId) => 
            await _context.Tenders.Where(t => t.UserId == userId).ToListAsync();
        public async Task<IEnumerable<Tender>> GetOpenTendersAsync() => 
            await _context.Tenders.Where(t => t.Status == Bolcko.Domain.Enums.TenderStatus.Open).ToListAsync();
    }

    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context) { }
        public async Task<IEnumerable<Project>> GetUserProjectsAsync(int userId) => 
            await _context.Projects.Where(p => p.UserId == userId).ToListAsync();
    }

    public class MarketPriceRepository : GenericRepository<MarketPrice>, IMarketPriceRepository
    {
        public MarketPriceRepository(ApplicationDbContext context) : base(context) { }
        public async Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName) => 
            await _context.MarketPrices.OrderByDescending(m => m.LastUpdated).FirstOrDefaultAsync(m => m.MaterialName == materialName);
    }
}