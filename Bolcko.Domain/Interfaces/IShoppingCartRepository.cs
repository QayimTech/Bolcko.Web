using Bolcko.Domain.Entities.ShoppingCart;

namespace Bolcko.Domain.Interfaces
{
    public interface IShoppingCartRepository
    {
        Task<ShoppingCart?> GetBySessionIdAsync(string sessionId);
        Task<ShoppingCart?> GetByUserIdAsync(int userId);
        Task<ShoppingCart?> GetByIdAsync(int id);
        Task<IEnumerable<ShoppingCart>> GetAllAsync();
        Task AddAsync(ShoppingCart shoppingCart);
        void Update(ShoppingCart shoppingCart);
        void Remove(ShoppingCart shoppingCart);
    }
}
