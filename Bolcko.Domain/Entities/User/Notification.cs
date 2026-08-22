using System;
using Bolcko.Domain.Common;

namespace Bolcko.Domain.Entities.User
{
    public class Notification : BaseEntity
    {
        public int? UserId { get; set; } // Null for system/broadcast or role-based notifications
        public User? User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
