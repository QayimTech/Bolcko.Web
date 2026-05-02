using Bolcko.Domain.Entities.Catalog;

namespace Blocko.Services.Interfaces.Category
{
    public interface IMarketPriceService
    {
        Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync();
        Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName);
    }
}
