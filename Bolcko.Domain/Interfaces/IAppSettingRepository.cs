using Bolcko.Domain.Entities.Setting;

namespace Bolcko.Domain.Interfaces
{
    public interface IAppSettingRepository : IGenericRepository<AppSetting>
    {
        Task<AppSetting?> GetByKeyAsync(string key);
    }
}
