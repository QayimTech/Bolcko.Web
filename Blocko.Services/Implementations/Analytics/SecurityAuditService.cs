using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Analytics;
using Bolcko.Domain.Entities.Analytics;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Services.Implementations.Analytics
{
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SecurityAuditService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;

            var now = DateTime.UtcNow;
            var blocked = await _unitOfWork.IpBlacklists.GetAllAsQueryable()
                .AsNoTracking()
                .AnyAsync(b => b.IpAddress == ipAddress && b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now));

            return blocked;
        }

        public async Task LogThreatAsync(string ipAddress, string path, string method, string threatType, string description, string? payload, string? userAgent)
        {
            var auditLog = new SecurityAuditLog
            {
                IpAddress = ipAddress,
                RequestPath = path,
                HttpMethod = method,
                ThreatType = threatType,
                Description = description,
                RequestPayload = payload,
                UserAgent = userAgent,
                DetectedAt = DateTime.UtcNow,
                IsBlocked = false,
                IsDismissed = false
            };

            await _unitOfWork.SecurityAuditLogs.AddAsync(auditLog);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetPendingThreatLogsAsync(int take = 50)
        {
            return await _unitOfWork.SecurityAuditLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(s => !s.IsDismissed)
                .OrderByDescending(s => s.DetectedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<IpBlacklist>> GetActiveBlacklistAsync()
        {
            var now = DateTime.UtcNow;
            return await _unitOfWork.IpBlacklists.GetAllAsQueryable()
                .AsNoTracking()
                .Where(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> BlacklistIpAsync(string ipAddress, string reason, string blockedByUserId, int? durationHours = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;

            var existing = await _unitOfWork.IpBlacklists.GetAllAsQueryable()
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);

            DateTime? expiresAt = durationHours.HasValue ? DateTime.UtcNow.AddHours(durationHours.Value) : null;

            if (existing != null)
            {
                existing.IsActive = true;
                existing.Reason = reason;
                existing.BlockedByUserId = blockedByUserId;
                existing.ExpiresAt = expiresAt;
            }
            else
            {
                var entry = new IpBlacklist
                {
                    IpAddress = ipAddress,
                    Reason = reason,
                    BlockedByUserId = blockedByUserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };
                await _unitOfWork.IpBlacklists.AddAsync(entry);
            }

            // Mark audit logs for this IP as blocked
            var auditLogs = await _unitOfWork.SecurityAuditLogs.GetAllAsQueryable()
                .Where(s => s.IpAddress == ipAddress)
                .ToListAsync();

            foreach (var log in auditLogs)
            {
                log.IsBlocked = true;
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> RemoveFromBlacklistAsync(string ipAddress)
        {
            var existing = await _unitOfWork.IpBlacklists.GetAllAsQueryable()
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);

            if (existing != null)
            {
                existing.IsActive = false;
                await _unitOfWork.CompleteAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> DismissThreatAsync(long auditLogId)
        {
            var log = await _unitOfWork.SecurityAuditLogs.GetByIdAsync((int)auditLogId);
            if (log != null)
            {
                log.IsDismissed = true;
                await _unitOfWork.CompleteAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ClearAllThreatLogsAndBlacklistAsync()
        {
            var allThreatLogs = await _unitOfWork.SecurityAuditLogs.GetAllAsQueryable().ToListAsync();
            foreach (var log in allThreatLogs)
            {
                _unitOfWork.SecurityAuditLogs.Remove(log);
            }

            var allBlacklist = await _unitOfWork.IpBlacklists.GetAllAsQueryable().ToListAsync();
            foreach (var item in allBlacklist)
            {
                _unitOfWork.IpBlacklists.Remove(item);
            }

            return await _unitOfWork.CompleteAsync() > 0;
        }
    }
}
