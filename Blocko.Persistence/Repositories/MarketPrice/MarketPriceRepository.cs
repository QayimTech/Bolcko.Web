using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.MarketPrice
{
    public class MarketPriceRepository : GenericRepository<Bolcko.Domain.Entities.Catalog.MarketPrice>, IMarketPriceRepository
    {
        public MarketPriceRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Bolcko.Domain.Entities.Catalog.MarketPrice?> GetLatestPriceByMaterialAsync(string materialName) => 
            await _context.MarketPrices.OrderByDescending(m => m.LastUpdated).FirstOrDefaultAsync(m => m.MaterialName == materialName);
    }
}