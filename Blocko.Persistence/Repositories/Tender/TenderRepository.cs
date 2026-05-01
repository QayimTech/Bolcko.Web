using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Tender
{
    public class TenderRepository : GenericRepository<Bolcko.Domain.Entities.Tender>, ITenderRepository
    {
        public TenderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Tender>> GetUserTendersAsync(int userId) => 
            await _context.Tenders.Where(t => t.UserId == userId).ToListAsync();

        public async Task<IEnumerable<Bolcko.Domain.Entities.Tender>> GetOpenTendersAsync() => 
            await _context.Tenders.Where(t => t.Status == Bolcko.Domain.Enums.TenderStatus.Open).ToListAsync();
    }
}