using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Delivery
{
    public class OrderShipment : BaseEntity
    {
        public int OrderId { get; set; }
        public Bolcko.Domain.Entities.Order.Order? Order { get; set; }
        
        public int? VendorId { get; set; }
        public string VendorKey { get; set; } = "qannas";
        public string VendorName { get; set; } = "مجموعة القنّاص لمواد البناء";

        public string ShipmentNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Processing;
        
        public string FulfillmentMode { get; set; } = "OwnFleet"; // OwnFleet, Custom3PL, PlatformPool
        public string? CarrierName { get; set; }
        public string? AssignedDriverName { get; set; }
        public string? AssignedDriverPhone { get; set; }
        public string? VehiclePlateNumber { get; set; }

        public decimal? WeighbridgeGrossKg { get; set; }
        public decimal? WeighbridgeTareKg { get; set; }
        public decimal? WeighbridgeNetKg { get; set; }
        public string? WeighbridgeTicketUrl { get; set; }
        public string? ManifestPdfUrl { get; set; }

        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount => SubtotalAmount + ShippingFee;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DispatchedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public ICollection<OrderShipmentItem> Items { get; set; } = new List<OrderShipmentItem>();
    }

    public class OrderShipmentItem : BaseEntity
    {
        public int OrderShipmentId { get; set; }
        public OrderShipment? OrderShipment { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? ProductVariantId { get; set; }
        public string? VariantDetails { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
