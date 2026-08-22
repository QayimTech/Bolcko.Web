using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Common;

namespace Blocko.Services.Interfaces.Order
{
    public interface IOrderService
    {
        Task<OrderDto> PlaceOrderAsync(int userId, ShoppingCartDto cart, Bolcko.Domain.Entities.Order.DTOs.CheckoutDto checkoutDto);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<IPagedList<OrderDto>> GetPagedOrdersAsync(int pageIndex, int pageSize, string? search = null, Bolcko.Domain.Enums.OrderStatus? status = null, string? sortOrder = null);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<bool> UpdateOrderStatusAsync(int id, Bolcko.Domain.Enums.OrderStatus status);

        /// <summary>Sum of TotalAmount for all orders.</summary>
        Task<decimal> GetTotalSalesAsync();

        /// <summary>Total number of orders in the system.</summary>
        Task<int> GetTotalCountAsync();
    }
}