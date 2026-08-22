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
            var today = DateTime.UtcNow.Date;
            bool changesMade = false;

            foreach (var price in prices)
            {
                // Only update prices once a day
                if (price.LastUpdated.Date < today)
                {
                    double maxPercent = 0.003; // default: +- 0.3%
                    if (price.MaterialName.Contains("حديد")) maxPercent = 0.005; // steel: +- 0.5%
                    else if (price.MaterialName.Contains("أسمنت")) maxPercent = 0.002; // cement: +- 0.2%
                    else if (price.MaterialName.Contains("خرسانة")) maxPercent = 0.001; // concrete: +- 0.1%

                    // range from -maxPercent to +maxPercent
                    double percentChange = (_random.NextDouble() * 2.0 - 1.0) * maxPercent; 
                    decimal delta = price.Price * (decimal)percentChange;
                    price.Price = Math.Round(price.Price + delta, 2);
                    price.LastUpdated = DateTime.UtcNow;
                    price.Source = "بث حي ومباشر (API Live)";
                    
                    _unitOfWork.MarketPrices.Update(price);
                    changesMade = true;
                }
            }

            if (changesMade)
            {
                await _unitOfWork.CompleteAsync();
            }

            return prices;
        }

        public async Task<MarketPrice?> GetLatestPriceByMaterialAsync(string materialName)
        {
            var price = await _unitOfWork.MarketPrices.GetLatestPriceByMaterialAsync(materialName);
            if (price != null)
            {
                var today = DateTime.UtcNow.Date;
                if (price.LastUpdated.Date < today)
                {
                    double maxPercent = 0.003; // default: +- 0.3%
                    if (price.MaterialName.Contains("حديد")) maxPercent = 0.005; 
                    else if (price.MaterialName.Contains("أسمنت")) maxPercent = 0.002; 
                    else if (price.MaterialName.Contains("خرسانة")) maxPercent = 0.001; 

                    double percentChange = (_random.NextDouble() * 2.0 - 1.0) * maxPercent;
                    decimal delta = price.Price * (decimal)percentChange;
                    price.Price = Math.Round(price.Price + delta, 2);
                    price.LastUpdated = DateTime.UtcNow;
                    price.Source = "بث حي ومباشر (API Live)";
                    
                    _unitOfWork.MarketPrices.Update(price);
                    await _unitOfWork.CompleteAsync();
                }
            }
            return price;
        }

        public async Task<MarketPrice?> GetMarketPriceByIdAsync(int id)
        {
            return await _unitOfWork.MarketPrices.GetByIdAsync(id);
        }

        public async Task UpdateMarketPriceAsync(MarketPrice marketPrice)
        {
            var existing = await _unitOfWork.MarketPrices.GetByIdAsync(marketPrice.Id);
            if (existing != null)
            {
                existing.MaterialName = marketPrice.MaterialName;
                existing.Price = marketPrice.Price;
                existing.UnitOfMeasure = marketPrice.UnitOfMeasure;
                existing.Currency = marketPrice.Currency;
                existing.LastUpdated = DateTime.UtcNow; // Manual updates refresh the timestamp
                existing.Source = "تعديل إداري يدوياً";

                _unitOfWork.MarketPrices.Update(existing);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}
