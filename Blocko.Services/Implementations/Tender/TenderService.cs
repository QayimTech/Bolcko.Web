using Blocko.Services.Interfaces.Tender;
using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Tender
{
    public class TenderService : ITenderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TenderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Bolcko.Domain.Entities.Tender.Tender> CreateTenderAsync(Bolcko.Domain.Entities.Tender.Tender tender)
        {
            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();
            return tender;
        }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetOpenTendersAsync() => await _unitOfWork.Tenders.GetOpenTendersAsync();
    }
}