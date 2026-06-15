using Bolcko.Domain.Entities.Order;

namespace Bolcko.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Bolcko.Domain.Entities.Order.Order> 
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId);
        Task<Bolcko.Domain.Entities.Order.Order?> GetOrderByIdWithItemsAsync(int id);
        Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersWithItemsAsync(int userId);
    }
}