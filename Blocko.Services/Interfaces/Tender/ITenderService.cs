using Bolcko.Domain.Entities.Tender.DTOs;

namespace Blocko.Services.Interfaces.Tender
{
    public interface ITenderService
    {
        Task<TenderDto> CreateTenderAsync(TenderDto tenderDto);
        Task<TenderDto> CreateQuoteRequestAsync(QuoteRequestDto quoteRequestDto, int? userId = null);
        Task<IEnumerable<TenderDto>> GetOpenTendersAsync();
        Task<IEnumerable<TenderDto>> GetTendersByUserAsync(int userId);
        Task<Bolcko.Domain.Entities.Tender.Tender?> GetTenderByIdAsync(int id);
        Task<bool> SubmitQuotationPricesAsync(int tenderId, Dictionary<int, decimal> itemPrices, string? notes);
        Task<bool> RequestPriceNegotiationAsync(int tenderId, Dictionary<int, decimal> targetPrices, string feedback);
        Task<bool> AcceptQuotationAsync(int tenderId);
        Task<bool> RejectTenderAsync(int tenderId, string reason);
    }
}