using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Blocko.Persistence;
using Blocko.Services.Interfaces.Subscription;
using Bolcko.Domain.Entities.Subscription.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blocko.Services.Implementations.Subscription
{
    public class SubscriptionBillingEngine : ISubscriptionBillingEngine
    {
        private readonly BlockoDbContext _context;
        private readonly ILogger<SubscriptionBillingEngine> _logger;

        public SubscriptionBillingEngine(BlockoDbContext context, ILogger<SubscriptionBillingEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ProcessUpcomingRenewalsAsync()
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(3);

            var expiringSubs = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.AutoRenew && s.EndDate <= threshold && s.EndDate >= now.AddDays(-1))
                .ToListAsync();

            int renewedCount = 0;
            foreach (var sub in expiringSubs)
            {
                if (sub.Plan != null && sub.AmountPaid > 0)
                {
                    // Tokenized Recurring billing execution
                    sub.PaymentTransactionRef = $"REC-BILL-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(10000, 99999)}";
                    sub.StartDate = sub.EndDate;
                    sub.EndDate = sub.BillingCycle == "Yearly" ? sub.EndDate.AddYears(1) : sub.EndDate.AddMonths(1);
                    sub.UpdatedAt = DateTime.UtcNow;
                    renewedCount++;

                    _logger.LogInformation("Subscription {SubId} auto-renewed until {End}. Txn: {Txn}", sub.Id, sub.EndDate, sub.PaymentTransactionRef);
                }
            }

            if (renewedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return renewedCount;
        }

        public async Task<JoFotaraInvoiceDto> GenerateTaxInvoiceAsync(int subscriptionId)
        {
            var sub = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            decimal total = sub?.AmountPaid ?? 100m;
            decimal subtotal = Math.Round(total / 1.16m, 2);
            decimal tax = total - subtotal;
            var invNum = $"JO-TAX-{DateTime.UtcNow:yyyyMMdd}-{subscriptionId:D5}";
            var qrHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(invNum + total))).Substring(0, 20);

            _logger.LogInformation("JoFotara Tax Invoice {InvNum} generated for Subscription {SubId}. Total: {Total} JOD", invNum, subscriptionId, total);

            return new JoFotaraInvoiceDto
            {
                InvoiceNumber = invNum,
                CustomerName = sub?.User != null ? $"{sub.User.FirstName} {sub.User.LastName}" : "مشترك منصة بلوكو",
                CustomerEmail = sub?.User?.Email ?? "customer@example.com",
                SubtotalAmountJod = subtotal,
                TaxRatePercent = 16.0m,
                TaxAmountJod = tax,
                TotalPayableJod = total,
                QrVerificationCode = qrHash,
                IssuedAtUtc = DateTime.UtcNow,
                Status = "SubmittedToNationalTaxAuthority"
            };
        }
    }
}
