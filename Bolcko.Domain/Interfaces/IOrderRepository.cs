using Bolcko.Domain.Entities.Order;

namespace Bolcko.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Bolcko.Domain.Entities.Order.Order> 
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId);
    }
}