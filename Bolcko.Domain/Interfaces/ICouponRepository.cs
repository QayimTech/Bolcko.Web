using Bolcko.Domain.Entities.Setting;

namespace Bolcko.Domain.Interfaces
{
    public interface ICouponRepository : IGenericRepository<Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);
    }
}
