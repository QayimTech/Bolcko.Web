using Blocko.Services.Interfaces.Financing;
using Bolcko.Web.App.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Jobs
{
    public interface IFinancingAutomationJobs
    {
        Task ProcessDailyRepaymentsAndYieldDistributionAsync();
    }

    public class FinancingAutomationJobs : IFinancingAutomationJobs
    {
        private readonly IFinancingService _financingService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<FinancingAutomationJobs> _logger;

        public FinancingAutomationJobs(
            IFinancingService financingService,
            IHubContext<NotificationHub> hubContext,
            ILogger<FinancingAutomationJobs> logger)
        {
            _financingService = financingService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task ProcessDailyRepaymentsAndYieldDistributionAsync()
        {
            _logger.LogInformation("Starting Hangfire automated daily repayment and yield distribution processing at {Time}", DateTime.UtcNow);

            try
            {
                int processedCount = await _financingService.ProcessDailyRepaymentsAndYieldDistributionAsync();

                if (processedCount > 0)
                {
                    _logger.LogInformation("Successfully processed and settled {Count} due financing tenders.", processedCount);

                    // Broadcast real-time update to Investors and Admins
                    await _hubContext.Clients.Group("Role_Investor").SendAsync("ReceiveYieldSettlementNotification", new
                    {
                        message = $"تم توزيع عوائد المرابحة وتسوية {processedCount} صفقة تمويل بنجاح في محافظكم الاستثمارية.",
                        timestamp = DateTime.UtcNow,
                        count = processedCount
                    });

                    await _hubContext.Clients.Group("Role_Admin").SendAsync("ReceiveAdminAlert", new
                    {
                        title = "تسوية صفقات التمويل اليومية",
                        message = $"قام المحرك الآلي بتسوية {processedCount} مناقصة تمويل وتوزيع أرباح المستثمرين.",
                        type = "FinancingSettlement"
                    });
                }
                else
                {
                    _logger.LogInformation("No matured financing tenders required settlement today.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Hangfire automated repayment & yield distribution execution.");
                throw; // Rethrow to let Hangfire track retry attempts
            }
        }
    }
}
