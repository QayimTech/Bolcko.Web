using Bolcko.Domain.Entities.Webhook;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Webhook
{
    public interface IOutboundWebhookService
    {
        Task<bool> PublishEventAsync(int vendorId, string eventType, object payload);
        Task<VendorWebhookConfig?> GetConfigByVendorIdAsync(int vendorId);
        Task<VendorWebhookConfig> SaveConfigAsync(int vendorId, string endpointUrl, string secretKey, bool isActive, string[] events);
        Task<IEnumerable<WebhookDeliveryLog>> GetDeliveryLogsAsync(int vendorId, int take = 20);
        string GenerateSecretKey();
        string ComputeHmacSha256Signature(string payload, string secret);
    }
}
