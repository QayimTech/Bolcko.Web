using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Subscription;
using Bolcko.Domain.Entities.Subscription.DTOs;

namespace Blocko.Services.Interfaces.Subscription
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync(string? targetRole = null);
        Task<SubscriptionPlanDto?> GetPlanByIdAsync(int planId);
        Task<UserSubscriptionDto?> GetUserActiveSubscriptionAsync(int userId);
        Task<IEnumerable<UserSubscriptionDto>> GetUserSubscriptionHistoryAsync(int userId);
        Task<bool> SubscribeAsync(int userId, SubscribeRequestDto request);
        Task<bool> CancelSubscriptionAsync(int userId, int subscriptionId);
        Task<decimal> GetEffectiveContractorMaxLimitAsync(int userId, decimal baseTrustScore);
        Task<decimal> GetEffectiveInvestorWakalaFeePercentAsync(int userId);
        Task<int> GetInvestorEarlyAccessHoursAsync(int userId);
        Task<SubscriptionStatsDto> GetSubscriptionStatsAsync();
        Task<IEnumerable<UserSubscriptionDto>> GetAllSubscribersAsync(string? role = null);
    }
}
