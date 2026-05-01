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
        
        Task<int> CompleteAsync();
    }
}