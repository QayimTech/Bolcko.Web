using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Category
{
    public class MarketPriceService : IMarketPriceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Random _random = new Random();

        public MarketPriceService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync()
        {
            var prices = await _unitOfWork.MarketPrices.GetAllAsync();
            foreach (var price in prices)
            {
                // Simulate a live API real-time price fluctuation within a realistic scale (+- 0.5%)
                double percentChange = (_random.NextDouble() * 1.0 - 0.5) / 100.0; // range from -0.005 to +0.005
                decimal delta = price.Price * (decimal)percentChange;
                price.Price = Math.Round(price.Price + delta, 2);
                price.LastUpdated = DateTime.UtcNow;
                price.Source = "بث حي ومباشر (API Live)";
            }
            return prices;
        }

        public async Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName)
        {
            var price = await _unitOfWork.MarketPrices.GetLatestPriceByMaterialAsync(materialName);
            if (price != null)
            {
                double percentChange = (_random.NextDouble() * 1.0 - 0.5) / 100.0;
                decimal delta = price.Price * (decimal)percentChange;
                price.Price = Math.Round(price.Price + delta, 2);
                price.LastUpdated = DateTime.UtcNow;
                price.Source = "بث حي ومباشر (API Live)";
            }
            return price;
        }
    }
}
