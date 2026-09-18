using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Subscription
{
    public class JoFotaraInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string TaxIdentificationNumber { get; set; } = "179402831"; // Block-O National Tax ID
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal SubtotalAmountJod { get; set; }
        public decimal TaxRatePercent { get; set; } = 16.0m;
        public decimal TaxAmountJod { get; set; }
        public decimal TotalPayableJod { get; set; }
        public string QrVerificationCode { get; set; } = string.Empty;
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "SubmittedToNationalTaxAuthority";
    }

    public interface ISubscriptionBillingEngine
    {
        Task<int> ProcessUpcomingRenewalsAsync();
        Task<JoFotaraInvoiceDto> GenerateTaxInvoiceAsync(int subscriptionId);
    }
}
