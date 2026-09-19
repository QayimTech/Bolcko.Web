using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Delivery.DTOs
{
    public enum VendorFulfillmentMode
    {
        OwnFleet = 1,      // أسطول المورد الخاص وسائقيه
        Custom3PL = 2,     // شركات نقل وشحن خارجية معتمدة (3PL)
        PlatformPool = 3   // شبكة أسطول شاحنات وروافع بلوكو الموحدة
    }

    public class VendorFulfillmentConfigDto
    {
        public int VendorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public VendorFulfillmentMode Mode { get; set; } = VendorFulfillmentMode.PlatformPool;

        // 3PL Integration Settings
        public string? ThirdPartyCarrierName { get; set; }
        public string? ThirdPartyAccountNo { get; set; }
        public string? ThirdPartyApiKey { get; set; }

        // Coverage & Rates (JOD)
        public List<string> CoveredGovernorates { get; set; } = new() { "عمان", "الزرقاء", "البلقاء", "إربد" };
        public decimal StandardDeliveryFeeJod { get; set; } = 25.00m;
        public decimal CraneOffloadingFeeJod { get; set; } = 35.00m;
        public decimal FreeDeliveryThresholdJod { get; set; } = 1500.00m;
        public int EstimatedSlaHours { get; set; } = 24;

        // Fleet Capacity
        public int TotalActiveTrucks { get; set; } = 3;
        public decimal MaxTruckPayloadTons { get; set; } = 30.0m;
        public bool HasCraneUnloadingEquipped { get; set; } = true;
    }

    public class VendorDispatchOrderDto
    {
        public int OrderId { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DestinationAddress { get; set; } = string.Empty;
        public string DestinationCity { get; set; } = "عمان";
        public double Latitude { get; set; } = 31.9539;
        public double Longitude { get; set; } = 35.9106;

        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "طن";
        public decimal TotalAmountJod { get; set; }

        // Fleet Dispatch
        public string? AssignedDriverName { get; set; }
        public string? AssignedDriverPhone { get; set; }
        public string? TruckPlateNumber { get; set; }
        public string? VehicleType { get; set; } // تريلا قلاب، رأس شاحنة، ونش تفريغ، لوري

        // Weighbridge
        public string? WeighbridgeTicketNo { get; set; }
        public decimal? TareWeightTons { get; set; }    // فارغ
        public decimal? GrossWeightTons { get; set; }   // محمل
        public decimal? NetWeightTons { get; set; }     // صافي المادة
        public string? WeighbridgeSlipUrl { get; set; }
        public DateTime? WeighedAt { get; set; }

        public string Status { get; set; } = "ReadyForLoading"; // ReadyForLoading, WeighedAndDispatched, InTransit, Delivered
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }

    public class VendorFleetVehicleDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal MaxCapacityTons { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public bool HasCrane { get; set; } = false;
    }
}
