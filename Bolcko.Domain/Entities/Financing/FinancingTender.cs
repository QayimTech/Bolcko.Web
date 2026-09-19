using Bolcko.Domain.Common;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Entities.User;
using System;
using System.Collections.Generic;

namespace Bolcko.Domain.Entities.Financing
{
    public class FinancingTender : BaseEntity
    {
        public int? ContractorId { get; set; }
        public Bolcko.Domain.Entities.User.User? Contractor { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public string ContractorPhone { get; set; } = string.Empty;
        public string? ContractorCompany { get; set; }

        public string TrackingCode { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectCity { get; set; } = "Amman";
        public string? ProjectAddress { get; set; }
        public string BuildingPermitNumber { get; set; } = string.Empty;
        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }

        // Financial & Murabaha Calculations
        public decimal BaseMaterialCost { get; set; }          // Net material purchase price to manufacturer
        public decimal ContractorMarkupRate { get; set; } = 0.06m; // Murabaha fixed profit rate (e.g. 6%)
        public decimal TotalPayableAmount { get; set; }        // Base + Markup total owed by contractor
        
        public decimal PlatformAgencyFeeRate { get; set; } = 0.015m; // 1.5% Wakala fee to Block-O
        public decimal PlatformAgencyFeeAmount { get; set; }
        public decimal InvestorNetYieldAmount { get; set; }    // Net profit credited to investor

        public int TenureDays { get; set; } = 45;              // 30, 45, or 60 days
        public DateTime? DueDate { get; set; }

        public FinancingTenderStatus Status { get; set; } = FinancingTenderStatus.OpenForBidding;

        // Funder / Investor Details
        public int? FunderInvestorId { get; set; }
        public Bolcko.Domain.Entities.User.User? FunderInvestor { get; set; }
        public string? FunderName { get; set; }
        public string? FunderPhone { get; set; }
        public string? WakalaContractPdfUrl { get; set; }

        // Security & Collateral
        public string? PromissoryNoteUrl { get; set; }
        public bool IsCollateralVerified { get; set; } = true;
        public double ContractorTrustScore { get; set; } = 95.0; // Trust index (0 - 100)

        // OTP Delivery & Verification (EP-01)
        public string? DeliveryOtpCode { get; set; }
        public DateTime? DeliveryOtpExpiresAt { get; set; }

        // Escrow & Settlement Control
        public string EscrowStatus { get; set; } = "HeldInEscrow"; // "HeldInEscrow", "ReleasedToVendor", "Refunded"
        public DateTime? EscrowReleasedAt { get; set; }
        public string? EscrowReleaseTransactionReference { get; set; }

        // Sharia Repayment & Murabaha Contract
        public string MurabahaContractStatus { get; set; } = "Draft"; // "Draft", "Funded", "ActiveRepayment", "Settled"
        public string? InstallmentScheduleJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FundedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? SettledAt { get; set; }

        public ICollection<FinancingTenderItem> Items { get; set; } = new List<FinancingTenderItem>();
        public ICollection<JobsiteProofOfDelivery> Deliveries { get; set; } = new List<JobsiteProofOfDelivery>();
    }
}