using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Product;

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

        Task<int> CompleteAsync();
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}