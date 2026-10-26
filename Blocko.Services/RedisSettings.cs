using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Blocko.Services
{
    /// <summary>
    /// Reads the Redis (ElastiCache) settings. Redis is used ONLY when something is configured;
    /// with nothing configured the app behaves exactly as before (in-memory cache, local keys).
    ///
    /// Environment variables (ECS task):
    ///   Redis__ConnectionString = master.xxx.cache.amazonaws.com:6379      (host:port, or a full StackExchange string)
    ///   Redis__Password         = AUTH token (only if the cluster has one)  - should come from Secrets Manager
    ///   Redis__UseTls           = true (default; ElastiCache with in-transit encryption needs TLS)
    ///   Redis__Endpoint         = alias of ConnectionString
    /// </summary>
    public static class RedisSettings
    {
        public const string DataProtectionKey = "Blocko:DataProtection-Keys";

        public static ConfigurationOptions? Build(IConfiguration config)
        {
            var conn = FirstSet(config["Redis:ConnectionString"], config["Redis:Endpoint"], config["REDIS_URL"]);
            if (string.IsNullOrWhiteSpace(conn)) return null;

            var options = ConfigurationOptions.Parse(conn!.Replace("rediss://", "").Replace("redis://", ""));
            var password = FirstSet(config["Redis:Password"], config["Redis:AuthToken"]);
            if (!string.IsNullOrWhiteSpace(password)) options.Password = password;

            var tlsSetting = config["Redis:UseTls"];
            var tls = string.IsNullOrWhiteSpace(tlsSetting) || !bool.TryParse(tlsSetting, out var b) || b;
            if (conn.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)) tls = false;
            options.Ssl = tls;

            options.AbortOnConnectFail = false;   // never stop the app from starting because Redis is slow
            options.ConnectTimeout = 5000;
            options.AbortOnConnectFail = false;
            options.ChannelPrefix = RedisChannel.Literal("blocko");
            return options;
        }

        public static bool IsConfigured(IConfiguration config) => Build(config) != null;

        private static string? FirstSet(params string?[] v) => v.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }
}
