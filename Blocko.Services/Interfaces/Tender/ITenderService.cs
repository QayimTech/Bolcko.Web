using Bolcko.Domain.Entities.Tender.DTOs;

namespace Blocko.Services.Interfaces.Tender
{
    public interface ITenderService
    {
        Task<TenderDto> CreateTenderAsync(TenderDto tenderDto);
        Task<TenderDto> CreateQuoteRequestAsync(QuoteRequestDto quoteRequestDto, int? userId = null);
        Task<IEnumerable<TenderDto>> GetOpenTendersAsync();
        Task<IEnumerable<TenderDto>> GetTendersByUserAsync(int userId);
        Task<TenderDto?> GetTenderByIdAsync(int id);
    }
}