using Bolcko.Domain.Entities.Setting;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Setting
{
    public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(BlockoDbContext context) : base(context)
        {
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            return await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToLower() == code.Trim().ToLower());
        }
    }
}
