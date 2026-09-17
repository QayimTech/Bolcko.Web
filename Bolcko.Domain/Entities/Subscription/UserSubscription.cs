using System;
using Bolcko.Domain.Entities.Subscription.Enums;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Entities.Subscription
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Bolcko.Domain.Entities.User.User? User { get; set; }

        public int PlanId { get; set; }
        public SubscriptionPlan? Plan { get; set; }

        public string BillingCycle { get; set; } = "Monthly"; // "Monthly", "Yearly"
        public decimal AmountPaid { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public bool AutoRenew { get; set; } = true;
        public string? PaymentTransactionRef { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
