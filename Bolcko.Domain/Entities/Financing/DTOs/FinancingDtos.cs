using System;
using System.Collections.Generic;
using Bolcko.Domain.Enums;

namespace Bolcko.Domain.Entities.Financing.DTOs
{
    public class CreateFinancingTenderRequestDto
    {
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectCity { get; set; } = "Amman";
        public string? ProjectAddress { get; set; }
        public string BuildingPermitNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Company { get; set; }
        public int TenureDays { get; set; } = 45; // 30, 45, 60
        public double Latitude { get; set; } = 31.9539;
        public double Longitude { get; set; } = 35.9106;
        
        public List<FinancingTenderItemDto> Items { get; set; } = new();
    }

    public class FinancingTenderItemDto
    {
        public int Id { get; set; }
        public string MaterialCategory { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPriceJod { get; set; }
        public decimal SubtotalJod { get; set; }
    }

    public class FinancingTenderDto
    {
        public int Id { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectCity { get; set; } = string.Empty;
        public string? ProjectAddress { get; set; }
        public string BuildingPermitNumber { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public string ContractorPhone { get; set; } = string.Empty;
        public string? ContractorCompany { get; set; }
        public double ContractorTrustScore { get; set; }

        public decimal BaseMaterialCost { get; set; }
        public decimal ContractorMarkupRate { get; set; }
        public decimal TotalPayableAmount { get; set; }
        public decimal PlatformAgencyFeeAmount { get; set; }
        public decimal InvestorNetYieldAmount { get; set; }
        public decimal AnnualizedYieldPercentage { get; set; } // (Yield / Base) * (365 / Tenure) * 100

        public int TenureDays { get; set; }
        public DateTime? DueDate { get; set; }
        public FinancingTenderStatus Status { get; set; }
        public string StatusNameAr { get; set; } = string.Empty;

        public string? FunderName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FundedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public List<FinancingTenderItemDto> Items { get; set; } = new();
        public List<JobsitePodDto> Deliveries { get; set; } = new();
    }

    public class FundTenderRequestDto
    {
        public int TenderId { get; set; }
        public string FunderName { get; set; } = string.Empty;
        public string FunderPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "CliQ"; // CliQ, BankTransfer, Wallet
        public bool AcceptWakalaTerms { get; set; }
    }

    public class JobsitePodDto
    {
        public int Id { get; set; }
        public int FinancingTenderId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public string VehiclePlateNumber { get; set; } = string.Empty;
        public double DistanceVarianceMeters { get; set; }
        public bool IsWithinGeoFence { get; set; }
        public string PhotoEvidenceUrl { get; set; } = string.Empty;
        public DateTime DeliveredAt { get; set; }
        public bool ContractorConfirmed { get; set; }
    }

    public class SubmitJobsitePodRequestDto
    {
        public int TenderId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public string VehiclePlateNumber { get; set; } = string.Empty;
        public double DriverLatitude { get; set; }
        public double DriverLongitude { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? PhotoUrl { get; set; }
    }
}