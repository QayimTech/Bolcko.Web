using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Order
{
    public class OrderRepository : GenericRepository<Bolcko.Domain.Entities.Order.Order>, IOrderRepository
    {
        public OrderRepository(BlockoDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId) => 
            await _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        public async Task<Bolcko.Domain.Entities.Order.Order?> GetOrderByIdWithItemsAsync(int id) =>
            await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersWithItemsAsync(int userId) =>
            await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
    }
}