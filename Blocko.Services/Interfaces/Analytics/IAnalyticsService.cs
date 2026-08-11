using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Analytics;

namespace Blocko.Services.Interfaces.Analytics
{
    public class TrafficKpiDto
    {
        public int TotalViews { get; set; }
        public int UniqueVisitors { get; set; }
        public int TotalViewsThisMonth { get; set; }
        public int UniqueVisitorsThisMonth { get; set; }
        public int ActiveLiveVisitors { get; set; }
        public int SuspiciousThreats { get; set; }
        public int ActiveBlockedIps { get; set; }
        public DateTime FilterStartDate { get; set; }
        public DateTime FilterEndDate { get; set; }
        public List<PageVisitStatDto> TopVisitedPages { get; set; } = new();
        public List<HourlyVisitStatDto> HourlyVisits { get; set; } = new();
    }

    public class PageVisitStatDto
    {
        public string Path { get; set; } = string.Empty;
        public int VisitCount { get; set; }
    }

    public class HourlyVisitStatDto
    {
        public string HourLabel { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public interface IAnalyticsService
    {
        Task RecordVisitAsync(string ipAddress, string path, string method, string? userAgent, string? referrer, string? userId, int statusCode, double executionTimeMs);
        Task<TrafficKpiDto> GetTrafficKpisAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
