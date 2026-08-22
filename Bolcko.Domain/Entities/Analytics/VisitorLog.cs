using System;

namespace Bolcko.Domain.Entities.Analytics
{
    public class VisitorLog
    {
        public long Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string? UserAgent { get; set; }
        public string? Referrer { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int StatusCode { get; set; } = 200;
        public double ExecutionTimeMs { get; set; }
    }
}
