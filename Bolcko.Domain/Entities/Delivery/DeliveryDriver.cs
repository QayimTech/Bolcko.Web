using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.User;
using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Delivery
{
    public class DeliveryDriver : BaseEntity
    {
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User User { get; set; } = null!;

        public int? DeliveryCompanyId { get; set; }
        public DeliveryCompany? DeliveryCompany { get; set; }

        // Freelancer & Oversized Tracking
        public bool HasTruck { get; set; } = true;
        public string TierLevel { get; set; } = "Bronze";
        public int TotalDeliveredOrders { get; set; } = 0;

        public string? VehicleType { get; set; }
        public string? VehiclePlateNumber { get; set; }
        public string? LicenseNumber { get; set; }

        // LOG-01 Heavy Hauler KYC Specifications
        public string? NationalId { get; set; }
        public string? HeavyLicenseCategory { get; set; } = "الفئة السادسة - قاطرة ومقطورة";
        public string? LicenseDocUrl { get; set; }
        public string? RegistrationDocUrl { get; set; }
        public string? VehiclePhotoUrl { get; set; }
        public string? CliqAlias { get; set; } // CliQ ID or IBAN for instant e-POD payouts
        public int CapacityTons { get; set; } = 15;
        public string? CoveredGovernorate { get; set; } = "كافة محافظات المملكة";
        public DateTime? ApprovedAt { get; set; }

        // Is the driver currently available to take jobs?
        public bool IsAvailable { get; set; } = true;

        // Is the driver approved by admin?
        public bool IsApproved { get; set; } = false;
        public string? RejectionReason { get; set; }

        public decimal AverageRating { get; set; } = 0.0m;
        public int TotalRatings { get; set; } = 0;

        public ICollection<DeliveryJob> Jobs { get; set; } = new List<DeliveryJob>();
        public ICollection<DeliveryBid> Bids { get; set; } = new List<DeliveryBid>();
        public ICollection<DeliveryRating> Ratings { get; set; } = new List<DeliveryRating>();
    }
}
