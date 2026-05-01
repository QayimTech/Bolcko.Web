using Bolcko.Domain.Entities.Tender;

namespace Bolcko.Domain.Interfaces
{
    public interface ITenderRepository : IGenericRepository<Bolcko.Domain.Entities.Tender.Tender> 
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetUserTendersAsync(int userId);
        Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetOpenTendersAsync();
    }
}