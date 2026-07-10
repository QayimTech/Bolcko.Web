using Bolcko.Domain.Entities.Setting;

namespace Bolcko.Domain.Interfaces
{
    public interface IShippingRateRepository : IGenericRepository<ShippingRate>
    {
        Task<ShippingRate?> GetByCityNameAsync(string cityName);
    }
}
