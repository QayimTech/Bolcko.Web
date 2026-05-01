using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Order
{
    public class OrderRepository : GenericRepository<Bolcko.Domain.Entities.Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Order>> GetUserOrdersAsync(int userId) => 
            await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
    }
}