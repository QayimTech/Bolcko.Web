using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Common;

namespace Blocko.Services.Interfaces.Order
{
    public interface IOrderService
    {
        Task<OrderDto> PlaceOrderAsync(OrderDto orderDto);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<IPagedList<OrderDto>> GetPagedOrdersAsync(int pageIndex, int pageSize);
        Task<OrderDto?> GetOrderByIdAsync(int id);
    }
}