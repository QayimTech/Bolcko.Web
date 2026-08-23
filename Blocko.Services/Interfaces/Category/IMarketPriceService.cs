using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Catalog.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Category
{
    public interface IMarketPriceService
    {
        Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync();
        Task<IEnumerable<MarketPriceDto>> GetMarketPricesDtoAsync();
        Task<LiveTickerDto> GetLiveTickerAsync();
        Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName);
        Task<MarketPrice?> GetMarketPriceByIdAsync(int id);
        Task UpdateMarketPriceAsync(MarketPrice marketPrice);
        Task SyncLiveGlobalMarketPricesAsync();
        Task<ConstructionEstimateResultDto> CalculateEstimateAsync(ConstructionEstimateRequestDto request);
    }
}
