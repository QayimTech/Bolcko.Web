using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Supplier;
using OrderEntity = Bolcko.Domain.Entities.Order.Order;

namespace Blocko.Services.Interfaces.Supplier
{
    public class SupplierOrderResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ExternalOrderId { get; set; }
        public string? ExternalOrderNumber { get; set; }
        public string? ErrorDetail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SupplierOrderStatusDto
    {
        public bool Success { get; set; }
        public string? ExternalOrderNumber { get; set; }
        public int OrderStatus { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? SalesApprovalStatusText { get; set; }
        public decimal TotalPrice { get; set; }
        public string? DeliveryAddress { get; set; }
    }

    public interface ISupplierApiService
    {
        Task<SupplierProviderConfig?> GetActiveConfigAsync(string supplierKey = "qannas");
        Task<List<SupplierProviderConfig>> GetAllConfigsAsync();
        Task<bool> SaveConfigAsync(SupplierProviderConfig config);
        Task<bool> TestConnectionAsync(SupplierProviderConfig config);
        Task<SupplierOrderResultDto> CreatePurchaseOrderAsync(OrderEntity order, SupplierProviderConfig? config = null);
        Task<SupplierOrderStatusDto> GetOrderStatusAsync(string externalOrderNumber, SupplierProviderConfig? config = null);
        Task<List<SupplierOrderStatusDto>> SyncOrdersStatusAsync(SupplierProviderConfig? config = null);
    }
}
