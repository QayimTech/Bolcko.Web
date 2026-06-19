using Blocko.Services.Interfaces.Order;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Common;
using Blocko.Persistence.Common;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Entities.User;

namespace Blocko.Services.Implementations.order
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public OrderService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<OrderDto> PlaceOrderAsync(int userId, ShoppingCartDto cart, CheckoutDto checkoutDto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var shippingAddress = new Address
                {
                    UserId = userId,
                    AddressLine1 = checkoutDto.DetailedAddress,
                    City = checkoutDto.City,
                    StateProvince = checkoutDto.Area,
                    AddressType = Bolcko.Domain.Enums.AddressType.Shipping
                };

                var billingAddress = new Address
                {
                    UserId = userId,
                    AddressLine1 = checkoutDto.DetailedAddress,
                    City = checkoutDto.City,
                    StateProvince = checkoutDto.Area,
                    AddressType = Bolcko.Domain.Enums.AddressType.Billing
                };

                await _unitOfWork.Addresses.AddAsync(shippingAddress);
                await _unitOfWork.Addresses.AddAsync(billingAddress);
                await _unitOfWork.CompleteAsync(); // To get Address IDs

                var order = new Order
                {
                    OrderNumber = $"BLK-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}",
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = cart.Total,
                    Status = Bolcko.Domain.Enums.OrderStatus.Pending,
                    PaymentMethod = checkoutDto.PaymentMethod,
                    PaymentStatus = "Pending",
                    ShippingAddressId = shippingAddress.Id,
                    BillingAddressId = billingAddress.Id,
                    Items = cart.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Subtotal = i.TotalPrice
                    }).ToList()
                };

                await _unitOfWork.Orders.AddAsync(order);

                // Deduct stock
                foreach (var item in cart.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (product.StockQuantity < 0) product.StockQuantity = 0;
                        _unitOfWork.Products.Update(product);
                    }
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
                return new OrderDto { Id = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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
            var o = await _unitOfWork.Orders.GetOrderByIdWithItemsAsync(id);
            if (o == null) return null;
            return new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                ShippingAddress = o.ShippingAddress != null ? new AddressDto
                {
                    AddressLine1 = o.ShippingAddress.AddressLine1,
                    AddressLine2 = o.ShippingAddress.AddressLine2,
                    City = o.ShippingAddress.City,
                    StateProvince = o.ShippingAddress.StateProvince,
                    PostalCode = o.ShippingAddress.PostalCode,
                    Country = o.ShippingAddress.Country
                } : null,
                Items = o.Items?.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.Subtotal
                }).ToList() ?? new List<OrderItemDto>()
            };
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, Bolcko.Domain.Enums.OrderStatus status)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null) return false;

            order.Status = status;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}