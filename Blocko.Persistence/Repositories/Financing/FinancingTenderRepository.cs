using Bolcko.Domain.Entities.Financing;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blocko.Persistence.Repositories.Financing
{
    public class FinancingTenderRepository : GenericRepository<FinancingTender>, IFinancingTenderRepository
    {
        public FinancingTenderRepository(BlockoDbContext context) : base(context)
        {
        }

        public async Task<FinancingTender?> GetTenderWithDetailsAsync(int id)
        {
            return await _context.Set<FinancingTender>()
                .Include(t => t.Items)
                .Include(t => t.Deliveries)
                .Include(t => t.Contractor)
                .Include(t => t.FunderInvestor)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<FinancingTender?> GetTenderByTrackingCodeAsync(string trackingCode)
        {
            return await _context.Set<FinancingTender>()
                .Include(t => t.Items)
                .Include(t => t.Deliveries)
                .FirstOrDefaultAsync(t => t.TrackingCode == trackingCode);
        }

        public async Task<IEnumerable<FinancingTender>> GetOpenTendersAsync()
        {
            return await _context.Set<FinancingTender>()
                .Include(t => t.Items)
                .Where(t => t.Status == FinancingTenderStatus.OpenForBidding)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FinancingTender>> GetTendersByContractorAsync(int contractorId)
        {
            return await _context.Set<FinancingTender>()
                .Include(t => t.Items)
                .Where(t => t.ContractorId == contractorId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FinancingTender>> GetTendersByFunderAsync(int funderId)
        {
            return await _context.Set<FinancingTender>()
                .Include(t => t.Items)
                .Where(t => t.FunderInvestorId == funderId)
                .OrderByDescending(t => t.FundedAt)
                .ToListAsync();
        }
    }
}