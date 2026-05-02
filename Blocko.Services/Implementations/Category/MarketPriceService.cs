using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Category
{
    public class MarketPriceService : IMarketPriceService
    {
        private readonly IUnitOfWork _unitOfWork;
        public MarketPriceService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync() => await _unitOfWork.MarketPrices.GetAllAsync();

        public async Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName) => 
            await _unitOfWork.MarketPrices.GetLatestPriceByMaterialAsync(materialName);
    }
}
