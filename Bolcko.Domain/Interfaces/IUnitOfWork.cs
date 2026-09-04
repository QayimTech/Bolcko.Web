using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Supplier;

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
        
        // Delivery & External Integration
        IGenericRepository<DeliveryCompany> DeliveryCompanies { get; }
        IGenericRepository<DeliveryDriver> DeliveryDrivers { get; }
        IGenericRepository<DeliveryJob> DeliveryJobs { get; }
        IGenericRepository<DeliveryBid> DeliveryBids { get; }
        IGenericRepository<DeliveryRating> DeliveryRatings { get; }
        IGenericRepository<DeliveryProviderConfig> DeliveryProviderConfigs { get; }
        IGenericRepository<OrderShipmentMapping> OrderShipmentMappings { get; }
        IGenericRepository<DeliveryProviderLocationMapping> DeliveryProviderLocationMappings { get; }
        IGenericRepository<SupplierProviderConfig> SupplierProviderConfigs { get; }

        // Analytics & Security
        IGenericRepository<Bolcko.Domain.Entities.Analytics.VisitorLog> VisitorLogs { get; }
        IGenericRepository<Bolcko.Domain.Entities.Content.FAQItem> FAQs { get; }
        IGenericRepository<Bolcko.Domain.Entities.Analytics.SecurityAuditLog> SecurityAuditLogs { get; }
        IGenericRepository<Bolcko.Domain.Entities.Analytics.IpBlacklist> IpBlacklists { get; }

        Task<int> CompleteAsync();
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
