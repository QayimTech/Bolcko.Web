using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces.Tender
{
    public interface ITenderService
    {
        Task<Bolcko.Domain.Entities.Tender> CreateTenderAsync(Bolcko.Domain.Entities.Tender tender);
        Task<IEnumerable<Bolcko.Domain.Entities.Tender>> GetOpenTendersAsync();
    }
}