using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Product;
using Blocko.Persistence.Repositories.Product;
using Blocko.Persistence.Repositories.Category;
using Blocko.Persistence.Repositories.Order;
using Blocko.Persistence.Repositories.Tender;
using Blocko.Persistence.Repositories.Project;
using Blocko.Persistence.Repositories.MarketPrice;
using Blocko.Persistence.Repositories.user;
using Blocko.Persistence.Repositories.SEO;
using Blocko.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Blocko.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BlockoDbContext _context;
        private IDbContextTransaction? _transaction;

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
            ShoppingCarts = new ShoppingCartRepository(_context);
            Addresses = new AddressRepository(_context);
            TenderItems = new GenericRepository<TenderItem>(_context);
            ShoppingCartItems = new GenericRepository<Bolcko.Domain.Entities.ShoppingCart.ShoppingCartItem>(_context);
            OrderItems = new GenericRepository<OrderItem>(_context);
            ProductImages = new GenericRepository<ProductImage>(_context);
            ProductVariants = new GenericRepository<ProductVariant>(_context);
            AppSettings = new Blocko.Persistence.Repositories.Setting.AppSettingRepository(_context);
            ShippingRates = new Blocko.Persistence.Repositories.Setting.ShippingRateRepository(_context);
            Coupons = new Blocko.Persistence.Repositories.Setting.CouponRepository(_context);
            
            // Delivery
            DeliveryCompanies = new GenericRepository<DeliveryCompany>(_context);
            DeliveryDrivers = new GenericRepository<DeliveryDriver>(_context);
            DeliveryJobs = new GenericRepository<DeliveryJob>(_context);
            DeliveryBids = new GenericRepository<DeliveryBid>(_context);
            DeliveryRatings = new GenericRepository<DeliveryRating>(_context);
        }

        public IUserRepository Users { get; private set; }
        public IProductRepository Products { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public ITenderRepository Tenders { get; private set; }
        public IProjectRepository Projects { get; private set; }
        public IMarketPriceRepository MarketPrices { get; private set; }
        public ISEORepository SEO { get; private set; }
        public IShoppingCartRepository ShoppingCarts { get; private set; }
        public IAddressRepository Addresses { get; private set; }
        public IGenericRepository<TenderItem> TenderItems { get; private set; }
        public IGenericRepository<Bolcko.Domain.Entities.ShoppingCart.ShoppingCartItem> ShoppingCartItems { get; private set; }
        public IGenericRepository<OrderItem> OrderItems { get; private set; }
        public IGenericRepository<ProductImage> ProductImages { get; private set; }
        public IGenericRepository<ProductVariant> ProductVariants { get; private set; }
        public IAppSettingRepository AppSettings { get; private set; }
        public IShippingRateRepository ShippingRates { get; private set; }
        public ICouponRepository Coupons { get; private set; }

        // Delivery
        public IGenericRepository<DeliveryCompany> DeliveryCompanies { get; private set; }
        public IGenericRepository<DeliveryDriver> DeliveryDrivers { get; private set; }
        public IGenericRepository<DeliveryJob> DeliveryJobs { get; private set; }
        public IGenericRepository<DeliveryBid> DeliveryBids { get; private set; }
        public IGenericRepository<DeliveryRating> DeliveryRatings { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}