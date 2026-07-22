using Blocko.Services.Interfaces.Order;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.Order.DTOs;
using Bolcko.Domain.Interfaces;
using Bolcko.Domain.Common;
using Blocko.Persistence.Common;
using Bolcko.Domain.Entities.ShoppingCart.DTOs;
using Bolcko.Domain.Entities.User;
using Blocko.Services.Interfaces.Notifications;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blocko.Services.Implementations.order
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly Blocko.Services.Interfaces.User.IEmailSender _emailSender;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> _userManager;

        public OrderService(
            IUnitOfWork unitOfWork, 
            INotificationService notificationService,
            Blocko.Services.Interfaces.User.IEmailSender emailSender,
            Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User> userManager)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _userManager = userManager;
        }

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

                decimal shippingFeeVal = 5.00m;
                var rateObj = await _unitOfWork.ShippingRates.GetByCityNameAsync(checkoutDto.City);
                if (rateObj != null)
                {
                    shippingFeeVal = rateObj.Rate;
                }
                else
                {
                    var generalSetting = await _unitOfWork.AppSettings.GetByKeyAsync("ShippingFee");
                    if (generalSetting != null && decimal.TryParse(generalSetting.Value, out decimal parsed))
                    {
                        shippingFeeVal = parsed;
                    }
                }

                // Coupon Calculations
                decimal discountAmt = 0.00m;
                string? appliedCoupon = null;
                if (!string.IsNullOrEmpty(checkoutDto.Notes) && checkoutDto.Notes.StartsWith("COUPON:"))
                {
                    var code = checkoutDto.Notes.Replace("COUPON:", "").Trim();
                    var coupon = await _unitOfWork.Coupons.GetByCodeAsync(code);
                    if (coupon != null && coupon.IsActive && (coupon.ExpiryDate == null || coupon.ExpiryDate > DateTime.UtcNow))
                    {
                        appliedCoupon = coupon.Code;
                        if (coupon.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                        {
                            discountAmt = Math.Round(cart.Subtotal * (coupon.DiscountValue / 100m), 2);
                        }
                        else
                        {
                            discountAmt = coupon.DiscountValue;
                        }
                        coupon.UsageCount++;
                        _unitOfWork.Coupons.Update(coupon);
                    }
                }

                var finalTotal = cart.Subtotal + cart.Tax + shippingFeeVal - discountAmt;
                if (finalTotal < 0) finalTotal = 0;

                var order = new Order
                {
                    OrderNumber = $"BLK-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}",
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = finalTotal,
                    Status = Bolcko.Domain.Enums.OrderStatus.Pending,
                    PaymentMethod = checkoutDto.PaymentMethod,
                    PaymentStatus = "Pending",
                    ShippingAddressId = shippingAddress.Id,
                    BillingAddressId = billingAddress.Id,
                    AppliedCouponCode = appliedCoupon,
                    DiscountAmount = discountAmt,
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

                // Send confirmation email to User
                try
                {
                    var user = await _userManager.FindByIdAsync(userId.ToString());
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailSubject = $"تأكيد استلام طلبك #{order.OrderNumber} - BLOCKO";
                        var detailsUrl = $"https://bolcko.com/Shop/Account/OrderDetails/{order.Id}"; // Staging/Production link structure
                        var emailBody = Blocko.Services.Helpers.EmailTemplates.GetOrderConfirmationTemplate(
                            order.OrderNumber,
                            $"{user.FirstName} {user.LastName}",
                            order.TotalAmount,
                            order.PaymentMethod ?? "COD",
                            detailsUrl
                        );
                        await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
                    }
                }
                catch
                {
                    // Fail silently so it doesn't break checkout flow
                }

                // Send notification to Admins
                try
                {
                    await _notificationService.SendNotificationToRoleAsync("Admin", "طلب جديد متاح", $"تم استلام طلب جديد رقم {order.OrderNumber} بقيمة {order.TotalAmount:N2} د.أ. اضغط للتفاصيل.", $"/Admin/Order/Details/{order.Id}");
                }
                catch
                {
                    // Fail silently so order placement doesn't crash if hub fails
                }

                return new OrderDto { Id = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount, AppliedCouponCode = order.AppliedCouponCode, DiscountAmount = order.DiscountAmount };
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

        public async Task<IPagedList<OrderDto>> GetPagedOrdersAsync(int pageIndex, int pageSize, string? search = null, Bolcko.Domain.Enums.OrderStatus? status = null, string? sortOrder = null)
        {
            System.Linq.Expressions.Expression<Func<Bolcko.Domain.Entities.Order.Order, bool>>? predicate = null;

            if (!string.IsNullOrWhiteSpace(search) || status.HasValue)
            {
                var s = search?.Trim().ToLower();
                predicate = o =>
                    (!status.HasValue || o.Status == status.Value) &&
                    (string.IsNullOrEmpty(s) ||
                     o.OrderNumber.ToLower().Contains(s) ||
                     (o.User != null && (o.User.FirstName.ToLower().Contains(s) || o.User.LastName.ToLower().Contains(s) || (o.User.Email != null && o.User.Email.ToLower().Contains(s)))) ||
                     (o.ShippingAddress != null && (o.ShippingAddress.City.ToLower().Contains(s) || o.ShippingAddress.AddressLine1.ToLower().Contains(s))));
            }

            Func<IQueryable<Bolcko.Domain.Entities.Order.Order>, IOrderedQueryable<Bolcko.Domain.Entities.Order.Order>> orderBy = sortOrder switch
            {
                "asc" => q => q.OrderBy(o => o.OrderDate),
                "name_asc" => q => q.OrderBy(o => o.OrderNumber),
                "name_desc" => q => q.OrderByDescending(o => o.OrderNumber),
                _ => q => q.OrderByDescending(o => o.OrderDate)
            };

            var pagedOrders = await _unitOfWork.Orders.GetPagedAsync(
                pageIndex,
                pageSize,
                predicate: predicate,
                orderBy: orderBy,
                includes: o => o.User!
            );

            var dtos = pagedOrders.Items.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                UserName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim() : o.User?.UserName,
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
                AppliedCouponCode = o.AppliedCouponCode,
                DiscountAmount = o.DiscountAmount,
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
                    ProductName = i.Product?.Name ?? $"منتج #{i.ProductId}",
                    Sku = i.ProductVariant?.Sku ?? i.Product?.Sku,
                    ImageUrl = i.ProductVariant?.ImageUrl ?? i.Product?.ImageUrl,
                    VariantInfo = i.ProductVariant != null ? string.Join(" | ", new[] { i.ProductVariant.Size, i.ProductVariant.Color, i.ProductVariant.PackagingUnit }.Where(s => !string.IsNullOrEmpty(s))) : null,
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

            // Send notification to the user
            try
            {
                var oldStatusAr = "غير معروف"; // If we had old status, but simple translation is fine
                var arStatus = status == Bolcko.Domain.Enums.OrderStatus.Pending ? "قيد الانتظار" :
                               status == Bolcko.Domain.Enums.OrderStatus.Processing ? "قيد المعالجة" :
                               status == Bolcko.Domain.Enums.OrderStatus.Shipped ? "تم الشحن" :
                               status == Bolcko.Domain.Enums.OrderStatus.Delivered ? "تم التوصيل" : "ملغي";

                var title = $"تحديث حالة الطلب #{order.OrderNumber}";
                var message = $"تم تحديث حالة طلبك إلى: {arStatus}. اضغط هنا للتفاصيل.";
                var actionUrl = $"/Shop/Account/OrderDetails/{order.Id}";
                await _notificationService.SendNotificationToUserAsync(order.UserId, title, message, actionUrl);

                // Send email update to customer
                var user = await _userManager.FindByIdAsync(order.UserId.ToString());
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var emailSubject = $"تحديث حالة طلبك #{order.OrderNumber} - BLOCKO";
                    var detailsUrl = $"https://bolcko.com/Shop/Account/OrderDetails/{order.Id}";
                    var emailBody = Blocko.Services.Helpers.EmailTemplates.GetOrderStatusTemplate(
                        order.OrderNumber,
                        "تحت المراجعة",
                        arStatus,
                        $"{user.FirstName} {user.LastName}",
                        detailsUrl
                    );
                    await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
                }
            }
            catch
            {
                // Fail silently
            }

            return true;
        }
    }
}