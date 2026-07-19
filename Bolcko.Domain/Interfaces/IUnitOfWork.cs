using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Delivery;

namespace Bolcko.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IOrderRepository Orders { get; }
        ITenderRepository Tenders { get; }
        IProjectRepository Projects { get; }
        IMarketPriceRepository MarketPrices { get; }
        ISEORepository SEO { get; }
        IShoppingCartRepository ShoppingCarts { get; }
        IGenericRepository<Bolcko.Domain.Entities.ShoppingCart.ShoppingCartItem> ShoppingCartItems { get; }
        IAddressRepository Addresses { get; }
        IGenericRepository<TenderItem> TenderItems { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<ProductImage> ProductImages { get; }
        IGenericRepository<ProductVariant> ProductVariants { get; }
        IAppSettingRepository AppSettings { get; }
        IShippingRateRepository ShippingRates { get; }
        ICouponRepository Coupons { get; }
        
        // Delivery
        IGenericRepository<DeliveryCompany> DeliveryCompanies { get; }
        IGenericRepository<DeliveryDriver> DeliveryDrivers { get; }
        IGenericRepository<DeliveryJob> DeliveryJobs { get; }
        IGenericRepository<DeliveryBid> DeliveryBids { get; }
        IGenericRepository<DeliveryRating> DeliveryRatings { get; }

        Task<int> CompleteAsync();
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}