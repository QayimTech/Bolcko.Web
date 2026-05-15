using Blocko.Services.Interfaces.Order;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Common;
using Blocko.Persistence.Common;

namespace Blocko.Services.Implementations.order
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<OrderDto> PlaceOrderAsync(OrderDto orderDto)
        {
            var order = new Order
            {
                UserId = orderDto.UserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = orderDto.TotalAmount,
                Status = orderDto.Status
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();
            orderDto.Id = order.Id;
            return orderDto;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);
            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus
            });
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus
            });
        }

        public async Task<IPagedList<OrderDto>> GetPagedOrdersAsync(int pageIndex, int pageSize)
        {
            var pagedOrders = await _unitOfWork.Orders.GetPagedAsync(
                pageIndex,
                pageSize,
                orderBy: q => q.OrderByDescending(o => o.OrderDate),
                includes: o => o.User!
            );

            var dtos = pagedOrders.Items.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus
            });

            return new PagedList<OrderDto>(dtos, pagedOrders.TotalCount, pageIndex, pageSize);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var o = await _unitOfWork.Orders.GetByIdAsync(id);
            if (o == null) return null;
            return new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus
            };
        }
    }
}