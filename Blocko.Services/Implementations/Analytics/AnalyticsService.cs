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
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RecordVisitAsync(string ipAddress, string path, string method, string? userAgent, string? referrer, string? userId, int statusCode, double executionTimeMs)
        {
            // Ignore static assets to keep analytics clean & accurate
            if (string.IsNullOrEmpty(path) ||
                path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var log = new VisitorLog
            {
                IpAddress = ipAddress,
                Path = path,
                Method = method,
                UserAgent = userAgent,
                Referrer = referrer,
                UserId = userId,
                StatusCode = statusCode,
                ExecutionTimeMs = executionTimeMs,
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.VisitorLogs.AddAsync(log);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<TrafficKpiDto> GetTrafficKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var now = DateTime.UtcNow;
            
            // Set default date range to Today if not specified and guarantee DateTimeKind.Utc for PostgreSQL compatibility
            DateTime rawStart = startDate?.Date ?? now.Date;
            DateTime rawEnd = endDate?.Date.AddDays(1).AddTicks(-1) ?? now.Date.AddDays(1).AddTicks(-1);

            DateTime start = DateTime.SpecifyKind(rawStart, DateTimeKind.Utc);
            DateTime end = DateTime.SpecifyKind(rawEnd, DateTimeKind.Utc);

            DateTime monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), DateTimeKind.Utc);
            DateTime activeWindow = DateTime.SpecifyKind(now.AddMinutes(-15), DateTimeKind.Utc);

            var filteredLogs = await _unitOfWork.VisitorLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(v => v.Timestamp >= start && v.Timestamp <= end)
                .ToListAsync();

            var monthLogsCount = await _unitOfWork.VisitorLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(v => v.Timestamp >= monthStart)
                .CountAsync();

            var monthUniqueVisitors = await _unitOfWork.VisitorLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(v => v.Timestamp >= monthStart)
                .Select(v => v.IpAddress)
                .Distinct()
                .CountAsync();

            var activeVisitors = await _unitOfWork.VisitorLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(v => v.Timestamp >= activeWindow)
                .Select(v => v.IpAddress)
                .Distinct()
                .CountAsync();

            var threatsCount = await _unitOfWork.SecurityAuditLogs.GetAllAsQueryable()
                .AsNoTracking()
                .Where(s => s.DetectedAt >= start && s.DetectedAt <= end)
                .CountAsync();

            var activeBlockedIps = await _unitOfWork.IpBlacklists.GetAllAsQueryable()
                .AsNoTracking()
                .Where(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now))
                .CountAsync();

            var topPages = filteredLogs
                .GroupBy(l => l.Path)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new PageVisitStatDto
                {
                    Path = g.Key,
                    VisitCount = g.Count()
                })
                .ToList();

            // Calculate hourly/daily chart data based on date range
            var hourlyStats = new List<HourlyVisitStatDto>();
            var daysDiff = (end - start).TotalDays;

            if (daysDiff <= 2)
            {
                // Hourly resolution
                int totalHours = (int)Math.Ceiling((end - start).TotalHours);
                totalHours = Math.Min(totalHours, 48);

                for (int i = totalHours - 1; i >= 0; i--)
                {
                    var hourTime = end.AddHours(-i);
                    var count = filteredLogs.Count(l => l.Timestamp.Hour == hourTime.Hour && l.Timestamp.Date == hourTime.Date);
                    hourlyStats.Add(new HourlyVisitStatDto
                    {
                        HourLabel = hourTime.ToString("HH:00"),
                        Count = count
                    });
                }
            }
            else
            {
                // Daily resolution for wider ranges
                int totalDays = (int)Math.Ceiling(daysDiff);
                for (int i = totalDays - 1; i >= 0; i--)
                {
                    var dayTime = end.Date.AddDays(-i);
                    var count = filteredLogs.Count(l => l.Timestamp.Date == dayTime.Date);
                    hourlyStats.Add(new HourlyVisitStatDto
                    {
                        HourLabel = dayTime.ToString("MM/dd"),
                        Count = count
                    });
                }
            }

            return new TrafficKpiDto
            {
                TotalViews = filteredLogs.Count,
                UniqueVisitors = filteredLogs.Select(l => l.IpAddress).Distinct().Count(),
                TotalViewsThisMonth = monthLogsCount,
                UniqueVisitorsThisMonth = monthUniqueVisitors,
                ActiveLiveVisitors = activeVisitors,
                SuspiciousThreats = threatsCount,
                ActiveBlockedIps = activeBlockedIps,
                FilterStartDate = start,
                FilterEndDate = end,
                TopVisitedPages = topPages,
                HourlyVisits = hourlyStats
            };
        }
    }
}
