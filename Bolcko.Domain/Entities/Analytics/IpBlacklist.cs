using System;

namespace Bolcko.Domain.Entities.Analytics
{
    public class IpBlacklist
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string BlockedByUserId { get; set; } = "System";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Null means permanent block
        public bool IsActive { get; set; } = true;
    }
}
