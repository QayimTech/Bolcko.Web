using Bolcko.Domain.Entities.Catalog;

namespace Bolcko.Domain.Interfaces
{
    public interface IMarketPriceRepository : IGenericRepository<MarketPrice> 
    {
        Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName);
    }
}