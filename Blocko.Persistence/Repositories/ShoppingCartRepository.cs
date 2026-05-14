using Bolcko.Domain.Entities.ShoppingCart;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly BlockoDbContext _context;

        public ShoppingCartRepository(BlockoDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart?> GetBySessionIdAsync(string sessionId)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(sc => sc.SessionId == sessionId);
        }

        public async Task<ShoppingCart?> GetByUserIdAsync(int userId)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(sc => sc.UserId == userId);
        }

        public async Task<ShoppingCart?> GetByIdAsync(int id)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<IEnumerable<ShoppingCart>> GetAllAsync()
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();
        }

        public async Task AddAsync(ShoppingCart shoppingCart)
        {
            await _context.ShoppingCarts.AddAsync(shoppingCart);
        }

        public void Update(ShoppingCart shoppingCart)
        {
            _context.ShoppingCarts.Update(shoppingCart);
        }

        public void Remove(ShoppingCart shoppingCart)
        {
            _context.ShoppingCarts.Remove(shoppingCart);
        }
    }
}
