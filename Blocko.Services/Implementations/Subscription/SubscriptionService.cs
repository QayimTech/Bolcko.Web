using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Blocko.Persistence;
using Blocko.Services.Interfaces.Subscription;
using Bolcko.Domain.Entities.Subscription;
using Bolcko.Domain.Entities.Subscription.DTOs;
using Bolcko.Domain.Entities.Subscription.Enums;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Services.Implementations.Subscription
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly BlockoDbContext _context;

        public SubscriptionService(BlockoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(string? targetRole = null)
        {
            var query = _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(targetRole))
            {
                query = query.Where(p => p.TargetRole.ToLower() == targetRole.ToLower());
            }

            var plans = await query
                .OrderBy(p => p.TargetRole)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync();

            return plans.Select(MapToDto);
        }

        public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(int planId)
        {
            var plan = await _context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId);

            return plan != null ? MapToDto(plan) : null;
        }

        public async Task<UserSubscriptionDto?> GetUserActiveSubscriptionAsync(int userId)
        {
            var sub = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            return sub != null ? MapToDto(sub) : null;
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetUserSubscriptionHistoryAsync(int userId)
        {
            var history = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return history.Select(MapToDto);
        }

        public async Task<bool> SubscribeAsync(int userId, SubscribeRequestDto request)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);
            if (plan == null) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            // Deactivate any existing active subscriptions for this user
            var existingSubs = await _context.UserSubscriptions
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                .ToListAsync();

            foreach (var ex in existingSubs)
            {
                ex.Status = SubscriptionStatus.Cancelled;
                ex.UpdatedAt = DateTime.UtcNow;
            }

            var isYearly = string.Equals(request.BillingCycle, "Yearly", StringComparison.OrdinalIgnoreCase);
            var price = isYearly ? plan.PriceYearly : plan.PriceMonthly;
            var endDate = isYearly ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);

            var newSub = new UserSubscription
            {
                UserId = userId,
                PlanId = plan.Id,
                BillingCycle = isYearly ? "Yearly" : "Monthly",
                AmountPaid = price,
                StartDate = DateTime.UtcNow,
                EndDate = endDate,
                Status = SubscriptionStatus.Active,
                AutoRenew = true,
                PaymentTransactionRef = $"TXN-SUB-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(newSub);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelSubscriptionAsync(int userId, int subscriptionId)
        {
            var sub = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

            if (sub == null) return false;

            sub.AutoRenew = false;
            sub.Status = SubscriptionStatus.Cancelled;
            sub.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetEffectiveContractorMaxLimitAsync(int userId, decimal baseTrustScore)
        {
            var activeSub = await GetUserActiveSubscriptionAsync(userId);
            if (activeSub != null && activeSub.MaxTenderLimit > 0)
            {
                return activeSub.MaxTenderLimit;
            }

            // Fallback to base trust score limit (Default starter: up to 10,000 JOD)
            var multiplier = baseTrustScore >= 80 ? 1.5m : (baseTrustScore >= 60 ? 1.0m : 0.5m);
            return 10000m * multiplier;
        }

        public async Task<decimal> GetEffectiveInvestorWakalaFeePercentAsync(int userId)
        {
            var standardFee = 1.5m;
            var activeSub = await GetUserActiveSubscriptionAsync(userId);
            if (activeSub != null && activeSub.CommissionDiscountPercent > 0)
            {
                return Math.Max(0.5m, standardFee - activeSub.CommissionDiscountPercent);
            }
            return standardFee;
        }

        public async Task<int> GetInvestorEarlyAccessHoursAsync(int userId)
        {
            var activeSub = await GetUserActiveSubscriptionAsync(userId);
            return activeSub?.EarlyAccessHours ?? 0;
        }

        public async Task<SubscriptionStatsDto> GetSubscriptionStatsAsync()
        {
            var activeSubs = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            var mrr = activeSubs.Sum(s => s.BillingCycle == "Yearly" ? (s.AmountPaid / 12) : s.AmountPaid);
            var totalRev = await _context.UserSubscriptions.SumAsync(s => s.AmountPaid);

            return new SubscriptionStatsDto
            {
                TotalActiveSubscribers = activeSubs.Count,
                InvestorSubscribers = activeSubs.Count(s => s.Plan?.TargetRole == "Investor"),
                ContractorSubscribers = activeSubs.Count(s => s.Plan?.TargetRole == "Contractor"),
                VendorSubscribers = activeSubs.Count(s => s.Plan?.TargetRole == "Vendor"),
                MonthlyRecurringRevenue = mrr,
                TotalRevenueToDate = totalRev
            };
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetAllSubscribersAsync(string? role = null)
        {
            var query = _context.UserSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(s => s.Plan != null && s.Plan.TargetRole.ToLower() == role.ToLower());
            }

            var list = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return list.Select(MapToDto);
        }

        private static SubscriptionPlanDto MapToDto(SubscriptionPlan p)
        {
            List<string> features = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(p.FeaturesJson))
                {
                    features = JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? new();
                }
            }
            catch { }

            return new SubscriptionPlanDto
            {
                Id = p.Id,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                DescriptionAr = p.DescriptionAr,
                DescriptionEn = p.DescriptionEn,
                TargetRole = p.TargetRole,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                MaxTenderLimit = p.MaxTenderLimit,
                CommissionDiscountPercent = p.CommissionDiscountPercent,
                EarlyAccessHours = p.EarlyAccessHours,
                AutoInvestAllowed = p.AutoInvestAllowed,
                Features = features,
                BadgeText = p.BadgeText,
                IsPopular = p.IsPopular,
                IsActive = p.IsActive,
                DisplayOrder = p.DisplayOrder
            };
        }

        private static UserSubscriptionDto MapToDto(UserSubscription s)
        {
            return new UserSubscriptionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = $"{s.User?.FirstName} {s.User?.LastName}".Trim(),
                UserEmail = s.User?.Email ?? string.Empty,
                TargetRole = s.Plan?.TargetRole ?? string.Empty,
                PlanId = s.PlanId,
                PlanNameAr = s.Plan?.NameAr ?? string.Empty,
                PlanNameEn = s.Plan?.NameEn ?? string.Empty,
                BillingCycle = s.BillingCycle,
                AmountPaid = s.AmountPaid,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                MaxTenderLimit = s.Plan?.MaxTenderLimit ?? 0,
                CommissionDiscountPercent = s.Plan?.CommissionDiscountPercent ?? 0,
                EarlyAccessHours = s.Plan?.EarlyAccessHours ?? 0,
                AutoInvestAllowed = s.Plan?.AutoInvestAllowed ?? false
            };
        }
    }
}
