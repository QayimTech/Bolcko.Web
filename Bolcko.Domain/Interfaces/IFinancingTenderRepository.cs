using Bolcko.Domain.Entities.Financing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bolcko.Domain.Interfaces
{
    public interface IFinancingTenderRepository : IGenericRepository<FinancingTender>
    {
        Task<FinancingTender?> GetTenderWithDetailsAsync(int id);
        Task<FinancingTender?> GetTenderByTrackingCodeAsync(string trackingCode);
        Task<IEnumerable<FinancingTender>> GetOpenTendersAsync();
        Task<IEnumerable<FinancingTender>> GetTendersByContractorAsync(int contractorId);
        Task<IEnumerable<FinancingTender>> GetTendersByFunderAsync(int funderId);
    }
}