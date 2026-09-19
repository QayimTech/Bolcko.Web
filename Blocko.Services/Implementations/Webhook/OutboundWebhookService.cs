using Blocko.Services.Interfaces.Webhook;
using Bolcko.Domain.Entities.Webhook;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Webhook
{
    public class OutboundWebhookService : IOutboundWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OutboundWebhookService> _logger;

        // In-memory persistent storage for configs and delivery logs
        private static readonly ConcurrentDictionary<int, VendorWebhookConfig> _configs = new();
        private static readonly ConcurrentDictionary<int, List<WebhookDeliveryLog>> _logs = new();

        public OutboundWebhookService(HttpClient httpClient, ILogger<OutboundWebhookService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Seed default mock config for testing vendor (VendorId = 1 / Al-Qannas)
            if (!_configs.ContainsKey(1))
            {
                _configs[1] = new VendorWebhookConfig
                {
                    Id = 1,
                    VendorId = 1,
                    VendorKey = "qannas",
                    EndpointUrl = "https://webhook.site/blocko-test-receiver",
                    SecretKey = "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
                    IsActive = true,
                    SubscribedEvents = "order.placed,order.escrow_funded,order.dispatch_ready,order.cancelled",
                    SuccessfulDeliveriesCount = 42,
                    FailedDeliveriesCount = 1
                };
            }
        }

        public async Task<bool> PublishEventAsync(int vendorId, string eventType, object payload)
        {
            var config = await GetConfigByVendorIdAsync(vendorId);
            if (config == null || !config.IsActive || string.IsNullOrWhiteSpace(config.EndpointUrl))
            {
                _logger.LogInformation("Webhook skipped for vendor {VendorId}: No active config.", vendorId);
                return false;
            }

            // Check if vendor subscribed to this event topic
            if (!string.IsNullOrEmpty(config.SubscribedEvents))
            {
                var events = config.SubscribedEvents.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!events.Contains(eventType, StringComparer.OrdinalIgnoreCase) && !events.Contains("*"))
                {
                    _logger.LogInformation("Webhook skipped for vendor {VendorId}: Not subscribed to event {EventType}.", vendorId, eventType);
                    return false;
                }
            }

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            var signature = ComputeHmacSha256Signature(jsonPayload, config.SecretKey);
            var deliveryId = Guid.NewGuid().ToString("N");
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var sw = Stopwatch.StartNew();
            int attempts = 0;
            int maxAttempts = 3;
            bool isSuccess = false;
            int? statusCode = null;
            string? responseContent = null;

            while (attempts < maxAttempts && !isSuccess)
            {
                attempts++;
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, config.EndpointUrl);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    request.Headers.Add("X-Blocko-Signature", $"sha256={signature}");
                    request.Headers.Add("X-Blocko-Event", eventType);
                    request.Headers.Add("X-Blocko-Delivery", deliveryId);
                    request.Headers.Add("X-Blocko-Timestamp", timestamp);

                    var response = await _httpClient.SendAsync(request);
                    statusCode = (int)response.StatusCode;
                    responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        isSuccess = true;
                    }
                    else if (attempts < maxAttempts)
                    {
                        // Exponential backoff
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempts)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Webhook attempt {Attempt} failed for vendor {VendorId} to {Url}", attempts, vendorId, config.EndpointUrl);
                    responseContent = ex.Message;
                    if (attempts < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempts)));
                    }
                }
            }

            sw.Stop();

            // Record Delivery Log
            var log = new WebhookDeliveryLog
            {
                VendorId = vendorId,
                EventType = eventType,
                EndpointUrl = config.EndpointUrl,
                PayloadJson = jsonPayload,
                SignatureHeader = $"sha256={signature}",
                ResponseStatusCode = statusCode,
                ResponseBody = responseContent?.Length > 500 ? responseContent[..500] : responseContent,
                AttemptCount = attempts,
                IsSuccess = isSuccess,
                DurationMs = sw.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };

            var vendorLogs = _logs.GetOrAdd(vendorId, _ => new List<WebhookDeliveryLog>());
            lock (vendorLogs)
            {
                vendorLogs.Insert(0, log);
                if (vendorLogs.Count > 50) vendorLogs.RemoveAt(vendorLogs.Count - 1);
            }

            if (isSuccess)
            {
                config.SuccessfulDeliveriesCount++;
            }
            else
            {
                config.FailedDeliveriesCount++;
            }
            config.LastTriggeredAt = DateTime.UtcNow;

            return isSuccess;
        }

        public Task<VendorWebhookConfig?> GetConfigByVendorIdAsync(int vendorId)
        {
            _configs.TryGetValue(vendorId, out var config);
            return Task.FromResult(config);
        }

        public Task<VendorWebhookConfig> SaveConfigAsync(int vendorId, string endpointUrl, string secretKey, bool isActive, string[] events)
        {
            var config = _configs.GetOrAdd(vendorId, id => new VendorWebhookConfig { VendorId = id });
            config.EndpointUrl = endpointUrl;
            config.SecretKey = string.IsNullOrWhiteSpace(secretKey) ? GenerateSecretKey() : secretKey;
            config.IsActive = isActive;
            config.SubscribedEvents = string.Join(",", events);
            config.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(config);
        }

        public Task<IEnumerable<WebhookDeliveryLog>> GetDeliveryLogsAsync(int vendorId, int take = 20)
        {
            if (_logs.TryGetValue(vendorId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IEnumerable<WebhookDeliveryLog>>(list.Take(take).ToList());
                }
            }

            // Seed mock recent logs if empty
            var mockLogs = new List<WebhookDeliveryLog>
            {
                new WebhookDeliveryLog
                {
                    VendorId = vendorId,
                    EventType = "order.placed",
                    EndpointUrl = "https://erp.alqannas-jo.com/api/webhooks/blocko",
                    PayloadJson = "{\"orderId\":1048,\"orderNumber\":\"ORD-260920-881\",\"totalAmount\":1450.00,\"status\":\"Processing\"}",
                    SignatureHeader = "sha256=9b7f0...ec1a",
                    ResponseStatusCode = 200,
                    ResponseBody = "{\"received\":true,\"erpBatchId\":\"ERP-9912\"}",
                    AttemptCount = 1,
                    IsSuccess = true,
                    DurationMs = 124,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-12)
                },
                new WebhookDeliveryLog
                {
                    VendorId = vendorId,
                    EventType = "order.escrow_funded",
                    EndpointUrl = "https://erp.alqannas-jo.com/api/webhooks/blocko",
                    PayloadJson = "{\"orderId\":1048,\"escrowStatus\":\"FundedByMurabahaInvestor\",\"amountJod\":1450.00}",
                    SignatureHeader = "sha256=1a2b3...c4d5",
                    ResponseStatusCode = 200,
                    ResponseBody = "{\"received\":true,\"status\":\"DISPATCH_AUTHORIZED\"}",
                    AttemptCount = 1,
                    IsSuccess = true,
                    DurationMs = 98,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            return Task.FromResult<IEnumerable<WebhookDeliveryLog>>(mockLogs);
        }

        public string GenerateSecretKey()
        {
            return "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        }

        public string ComputeHmacSha256Signature(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
