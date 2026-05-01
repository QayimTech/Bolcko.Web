using Bolcko.Domain.Interfaces;
using Blocko.Persistence.Repositories;

namespace Blocko.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Products = new ProductRepository(_context);
            Categories = new CategoryRepository(_context);
            Orders = new OrderRepository(_context);
            Tenders = new TenderRepository(_context);
            Projects = new ProjectRepository(_context);
            MarketPrices = new MarketPriceRepository(_context);
        }

        public IUserRepository Users { get; private set; }
        public IProductRepository Products { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public ITenderRepository Tenders { get; private set; }
        public IProjectRepository Projects { get; private set; }
        public IMarketPriceRepository MarketPrices { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}