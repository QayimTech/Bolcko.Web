using Bolcko.Domain.Entities.Setting;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Setting
{
    public class AppSettingRepository : GenericRepository<AppSetting>, IAppSettingRepository
    {
        public AppSettingRepository(BlockoDbContext context) : base(context)
        {
        }

        public async Task<AppSetting?> GetByKeyAsync(string key)
        {
            return await _context.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        }
    }
}
