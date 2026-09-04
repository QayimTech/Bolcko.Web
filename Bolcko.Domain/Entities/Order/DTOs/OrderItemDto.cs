namespace Bolcko.Domain.Entities.Order.DTOs
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public string? VariantInfo { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? SupplierKey { get; set; }
        public int? ExternalSupplierVariantId { get; set; }
        public string? ExternalSupplierOrderId { get; set; }
        public Bolcko.Domain.Enums.SourcingStatus SourcingStatus { get; set; } = Bolcko.Domain.Enums.SourcingStatus.PendingSourcing;
    }
}
