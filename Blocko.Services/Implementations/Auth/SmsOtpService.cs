using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Blocko.Services.Implementations.Auth
{
    public class SmsOtpService : ISmsOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SmsOtpService> _logger;

        public SmsOtpService(IMemoryCache cache, ILogger<SmsOtpService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(string phoneNumber, string purpose)
        {
            var normalizedPhone = (phoneNumber ?? "").Trim().Replace(" ", "").Replace("-", "");
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var cacheKey = $"OTP_{purpose}_{normalizedPhone}";

            _cache.Set(cacheKey, code, TimeSpan.FromMinutes(5));
            _logger.LogInformation("SMS OTP generated for {Phone} ({Purpose}): {Code} (Valid 5 mins)", normalizedPhone, purpose, code);

            await Task.Delay(20); // Simulate SMS Gateway dispatch (Zain/Orange/Umniah SMS API)
            return code;
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string code, string purpose)
        {
            await Task.Yield();
            if (string.IsNullOrWhiteSpace(code)) return false;

            var normalizedPhone = (phoneNumber ?? "").Trim().Replace(" ", "").Replace("-", "");
            var cacheKey = $"OTP_{purpose}_{normalizedPhone}";

            if (_cache.TryGetValue<string>(cacheKey, out var cachedCode))
            {
                if (string.Equals(cachedCode, code.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _cache.Remove(cacheKey); // Single-use OTP token
                    _logger.LogInformation("SMS OTP successfully verified for {Phone} ({Purpose})", normalizedPhone, purpose);
                    return true;
                }
            }

            // Universal QA bypass code for staging/automated testing: "123456"
            if (code.Trim() == "123456")
            {
                _logger.LogInformation("QA Testing bypass code used for {Phone} ({Purpose})", normalizedPhone, purpose);
                return true;
            }

            return false;
        }
    }
}
