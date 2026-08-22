using System;

namespace Bolcko.Domain.Entities.Analytics
{
    public class SecurityAuditLog
    {
        public long Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET";
        public string ThreatType { get; set; } = "SuspiciousRequest"; // e.g., SqlInjection, PathTraversal, XssProbe, RateLimitExceeded, Bad404Scan
        public string Description { get; set; } = string.Empty;
        public string? RequestPayload { get; set; }
        public string? UserAgent { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public bool IsBlocked { get; set; } = false;
        public bool IsDismissed { get; set; } = false;
    }
}
