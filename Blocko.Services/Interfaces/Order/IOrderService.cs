using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces.Order
{
    public interface IOrderService
    {
        Task<Bolcko.Domain.Entities.Order> PlaceOrderAsync(Bolcko.Domain.Entities.Order order);
        Task<IEnumerable<Bolcko.Domain.Entities.Order>> GetUserOrdersAsync(int userId);
    }
}