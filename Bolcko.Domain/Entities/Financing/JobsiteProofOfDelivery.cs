using Bolcko.Domain.Common;
using System;

namespace Bolcko.Domain.Entities.Financing
{
    public class JobsiteProofOfDelivery : BaseEntity
    {
        public int FinancingTenderId { get; set; }
        public FinancingTender? FinancingTender { get; set; }

        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public string VehiclePlateNumber { get; set; } = string.Empty;

        public double DriverLatitude { get; set; }
        public double DriverLongitude { get; set; }
        public double TargetJobsiteLatitude { get; set; }
        public double TargetJobsiteLongitude { get; set; }
        public double DistanceVarianceMeters { get; set; }
        public bool IsWithinGeoFence { get; set; } // true if <= 150m

        public bool DispatcherOverride { get; set; }
        public string? DispatcherOverrideNotes { get; set; }

        public string PhotoEvidenceUrl { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

        public bool ContractorConfirmed { get; set; } = true;
        public DateTime? ContractorSignOffTime { get; set; }
        public string? ContractorDigitalSignatureUrl { get; set; }
    }
}