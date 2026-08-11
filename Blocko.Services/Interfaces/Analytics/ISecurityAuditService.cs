using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Analytics;

namespace Blocko.Services.Interfaces.Analytics
{
    public interface ISecurityAuditService
    {
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task LogThreatAsync(string ipAddress, string path, string method, string threatType, string description, string? payload, string? userAgent);
        Task<IEnumerable<SecurityAuditLog>> GetPendingThreatLogsAsync(int take = 50);
        Task<IEnumerable<IpBlacklist>> GetActiveBlacklistAsync();
        Task<bool> BlacklistIpAsync(string ipAddress, string reason, string blockedByUserId, int? durationHours = null);
        Task<bool> RemoveFromBlacklistAsync(string ipAddress);
        Task<bool> DismissThreatAsync(long auditLogId);
    }
}
