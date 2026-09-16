using Bolcko.Domain.Entities.Financing.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Financing
{
    public interface IFinancingService
    {
        Task<FinancingTenderDto> CreateTenderFromBOQAsync(CreateFinancingTenderRequestDto request, int? userId = null);
        Task<IEnumerable<FinancingTenderDto>> GetOpenTendersAsync();
        Task<FinancingTenderDto?> GetTenderByIdAsync(int id);
        Task<FinancingTenderDto?> GetTenderByTrackingCodeAsync(string trackingCode);
        Task<bool> FundTenderAsync(FundTenderRequestDto request, int? funderId = null);
        Task<JobsitePodDto> SubmitJobsitePodAsync(SubmitJobsitePodRequestDto request);
        Task<bool> SettleTenderAsync(int tenderId);
        double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2);
        Task<ContractorDashboardDto> GetContractorDashboardAsync(int? userId, string? phone = null);
        Task<InvestorDashboardDto> GetInvestorDashboardAsync(int? userId, string? phone = null);
        Task<AdminFinancingOverviewDto> GetAdminFinancingOverviewAsync();
    }
}