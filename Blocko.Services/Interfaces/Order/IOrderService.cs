namespace Blocko.Services.Interfaces.Order
{
    public interface IOrderService
    {
        Task<Bolcko.Domain.Entities.Order.Order> PlaceOrderAsync(Bolcko.Domain.Entities.Order.Order order);
        Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetAllOrdersAsync();
        Task<Bolcko.Domain.Entities.Order.Order?> GetOrderByIdAsync(int id);
    }
}