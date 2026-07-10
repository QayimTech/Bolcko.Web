using Bolcko.Domain.Entities.Setting;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Setting
{
    public class ShippingRateRepository : GenericRepository<ShippingRate>, IShippingRateRepository
    {
        public ShippingRateRepository(BlockoDbContext context) : base(context)
        {
        }

        public async Task<ShippingRate?> GetByCityNameAsync(string cityName)
        {
            return await _context.ShippingRates.FirstOrDefaultAsync(s => s.CityName == cityName);
        }
    }
}
