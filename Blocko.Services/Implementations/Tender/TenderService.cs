using Blocko.Services.Interfaces.Tender;
using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Tender
{
    public class TenderService : ITenderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TenderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Tender> CreateTenderAsync(Tender tender)
        {
            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();
            return tender;
        }

        public async Task<IEnumerable<Tender>> GetOpenTendersAsync() => await _unitOfWork.Tenders.GetOpenTendersAsync();

        public async Task<IEnumerable<Tender>> GetTendersByUserAsync(int userId) => await _unitOfWork.Tenders.FindAsync(t => t.UserId == userId);

        public async Task<Tender?> GetTenderByIdAsync(int id) => await _unitOfWork.Tenders.GetByIdAsync(id);
    }
}