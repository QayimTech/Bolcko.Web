using Bolcko.Domain.Entities.Tender;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Tender
{
    public class TenderRepository : GenericRepository<Bolcko.Domain.Entities.Tender.Tender>, ITenderRepository
    {
        public TenderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetUserTendersAsync(int userId) => 
            await _context.Tenders.Where(t => t.UserId == userId).ToListAsync();

        public async Task<IEnumerable<Bolcko.Domain.Entities.Tender.Tender>> GetOpenTendersAsync() => 
            await _context.Tenders.Where(t => t.Status == Bolcko.Domain.Enums.TenderStatus.Open).ToListAsync();
    }
}