using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Delivery;
using System;

namespace Bolcko.Domain.Entities.Financing
{
    /// <summary>
    /// LOG-06: OTP-Guarded Jobsite Delivery & Instant CliQ Carrier Wallet Payout
    /// Handles instant escrow release and liquidity transfer to independent drivers and 3PL carriers upon verified e-POD.
    /// </summary>
    public class CarrierPayoutTransaction : BaseEntity
    {
        public int? DeliveryJobId { get; set; }
        public DeliveryJob? DeliveryJob { get; set; }

        public int? DriverId { get; set; }
        public DeliveryDriver? Driver { get; set; }

        public int? DeliveryCompanyId { get; set; }
        public DeliveryCompany? DeliveryCompany { get; set; }

        public int OrderId { get; set; }

        // Financial & Escrow Breakdown (JOD)
        public decimal GrossFreightAmountJod { get; set; } = 0.00m;
        public decimal PlatformTakeRateFee { get; set; } = 0.00m;
        public decimal PayoutAmountJod { get; set; } = 0.00m;

        // Payment Channel & Beneficiary
        public string PayoutMethod { get; set; } = "CliQ"; // "CliQ", "CarrierWallet", "BankTransfer"
        public string? DestinationCliqAlias { get; set; }
        public string? DestinationBankIban { get; set; }
        public string TransactionReference { get; set; } = string.Empty; // e.g., CLIQ-TXN-2026-XXXXX

        // Status & Lifecycle
        // "Completed", "Pending", "FrozenDisputed", "Failed"
        public string Status { get; set; } = "Completed";

        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SettledAt { get; set; }

        // Security & Verification Proof
        public string? DeliveryOtpVerified { get; set; }
        public string? EpodDocumentUrl { get; set; }
        public bool IsEscrowReleased { get; set; } = false;

        // Dispute & Fraud Protection
        public string? DisputeReason { get; set; }
        public string? DisputePhotoEvidenceUrl { get; set; }
        public DateTime? DisputedAt { get; set; }
    }
}
