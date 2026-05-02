namespace Blocko.Services.Interfaces.Tender
{
    public interface ITenderService
    {
        Task<Bolcko.Domain.Entities.Tender.Tender> CreateTenderAsync(Bolcko.Domain.Entities.Tender.Tender tender);
        Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetOpenTendersAsync();
        Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetTendersByUserAsync(int userId);
        Task<Bolcko.Domain.Entities.Tender.Tender?> GetTenderByIdAsync(int id);
    }
}