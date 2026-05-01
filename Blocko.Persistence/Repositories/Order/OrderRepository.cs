using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Order
{
    public class OrderRepository : GenericRepository<Bolcko.Domain.Entities.Order.Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId) => 
            await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
    }
}