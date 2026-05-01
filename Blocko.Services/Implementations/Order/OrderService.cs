using Blocko.Services.Interfaces.Order;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Order
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Bolcko.Domain.Entities.Order.Order> PlaceOrderAsync(Bolcko.Domain.Entities.Order.Order order)
        {
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();
            return order;
        }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Order.Order>> GetUserOrdersAsync(int userId) => await _unitOfWork.Orders.GetUserOrdersAsync(userId);
    }
}