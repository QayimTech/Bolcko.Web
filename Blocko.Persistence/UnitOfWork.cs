using Bolcko.Domain.Interfaces;
using Blocko.Persistence.Repositories.Product;
using Blocko.Persistence.Repositories.Category;
using Blocko.Persistence.Repositories.Order;
using Blocko.Persistence.Repositories.Tender;
using Blocko.Persistence.Repositories.Project;
using Blocko.Persistence.Repositories.MarketPrice;
using Blocko.Persistence.Repositories.user;
using Blocko.Persistence.Repositories.SEO;

namespace Blocko.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BlockoDbContext _context;

        public UnitOfWork(BlockoDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Products = new ProductRepository(_context);
            Categories = new CategoryRepository(_context);
            Orders = new OrderRepository(_context);
            Tenders = new TenderRepository(_context);
            Projects = new ProjectRepository(_context);
            MarketPrices = new MarketPriceRepository(_context);
            SEO = new SEORepositroy(_context);
        }

        public IUserRepository Users { get; private set; }
        public IProductRepository Products { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public ITenderRepository Tenders { get; private set; }
        public IProjectRepository Projects { get; private set; }
        public IMarketPriceRepository MarketPrices { get; private set; }
        public ISEORepository SEO { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}