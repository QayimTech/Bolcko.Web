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

        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }

        public string? FunderName { get; set; }
        public string? FunderPhone { get; set; }
        public string? WakalaContractPdfUrl { get; set; }
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
        public string? FunderEmail { get; set; }
        public string? Password { get; set; }
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

    public class ContractorDashboardDto
    {
        public string ContractorName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Phone { get; set; } = string.Empty;
        public double TrustScore { get; set; } = 95.0;
        public string TrustTier { get; set; } = "بلاتيني (Platinum)"; // بلاتيني, ذهبي, فضي
        public decimal CreditLimitJod { get; set; } = 50000m;
        public decimal CreditUtilizedJod { get; set; }
        public decimal AvailableCreditJod => Math.Max(0, CreditLimitJod - CreditUtilizedJod);

        public int TotalTendersCount { get; set; }
        public int ActiveTendersCount { get; set; }
        public int SettledTendersCount { get; set; }
        public decimal TotalFinancedAmountJod { get; set; }
        public decimal TotalSettledAmountJod { get; set; }

        public List<FinancingTenderDto> ActiveTenders { get; set; } = new();
        public List<PaymentScheduleItemDto> PaymentSchedule { get; set; } = new();
    }

    public class PaymentScheduleItemDto
    {
        public int TenderId { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public decimal AmountDueJod { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsOverdue => DaysRemaining < 0;
        public FinancingTenderStatus Status { get; set; }
    }

    public class InvestorDashboardDto
    {
        public string InvestorName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal TotalInvestedJod { get; set; }
        public decimal RealizedProfitJod { get; set; }
        public decimal ExpectedUpcomingProfitJod { get; set; }
        public decimal AverageAnnualizedYieldPercentage { get; set; }
        public int ActiveDealsCount { get; set; }
        public int CompletedDealsCount { get; set; }

        public decimal AvailableWalletBalanceJod { get; set; }
        public decimal PendingPayoutRequestsJod { get; set; }
        public decimal TotalWithdrawnJod { get; set; }

        public List<FinancingTenderDto> ActiveInvestments { get; set; } = new();
        public List<FinancingTenderDto> CompletedInvestments { get; set; } = new();
        public List<InvestorWalletTransactionDto> WalletLedger { get; set; } = new();
    }

    public class InvestorWalletTransactionDto
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal AmountJod { get; set; }
        public string Type { get; set; } = "ProfitCredit"; // Deposit, ProfitCredit, PayoutWithdrawal
        public string PaymentChannel { get; set; } = "CliQ"; // CliQ, BankIBAN, EscrowRelease
        public string Status { get; set; } = "Completed"; // Completed, Pending, Processing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReferenceCode { get; set; }
    }

    public class InvestorPayoutRequestDto
    {
        public decimal AmountJod { get; set; }
        public string PayoutMethod { get; set; } = "CliQ"; // CliQ, BankTransfer
        public string? CliqAlias { get; set; }
        public string? IbanNumber { get; set; }
        public string? BankName { get; set; }
        public string? Notes { get; set; }
    }

    public class AdminFinancingOverviewDto
    {
        public decimal TotalFacilitatedFinancingJod { get; set; }
        public decimal TotalPlatformFeesCollectedJod { get; set; }
        public decimal TotalInvestorProfitsDistributedJod { get; set; }
        public int OpenTendersCount { get; set; }
        public int InTransitCount { get; set; }
        public int DeliveredPendingSettlementCount { get; set; }
        public int SettledCount { get; set; }

        public List<FinancingTenderDto> AllTenders { get; set; } = new();
    }
}