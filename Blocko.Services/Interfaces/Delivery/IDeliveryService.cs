using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Enums;

namespace Blocko.Services.Interfaces.Delivery
{
    public interface IDeliveryService
    {
        // Companies
        Task<DeliveryCompany> CreateCompanyAsync(
            string name,
            string? email,
            string? phoneNumber,
            string? commercialRegister,
            decimal baseRate,
            string? managerUserId = null,
            string? taxId = null,
            string? transportCommissionLicense = null,
            string? commercialRegisterDocUrl = null,
            string? transportLicenseDocUrl = null,
            string? cliqAlias = null,
            int totalTrucksCount = 5,
            bool isApproved = false);
        Task<IEnumerable<DeliveryCompany>> GetActiveCompaniesAsync();
        Task DeleteCompanyAsync(int companyId);
        Task<IEnumerable<DeliveryCompany>> GetAllCompaniesAsync();
        Task<Bolcko.Domain.Common.IPagedList<DeliveryCompany>> GetPagedCompaniesAsync(int pageIndex, int pageSize);
        Task<DeliveryCompany?> GetCompanyByIdAsync(int companyId);
        Task ToggleCompanyStatusAsync(int companyId);
        Task<DeliveryCompany?> GetCompanyByManagerUserIdAsync(string managerUserId);
        Task<IEnumerable<DeliveryJob>> GetCompanyJobsAsync(int companyId);
        Task UpdateCompanyJobCollectedAmountAsync(int jobId, decimal collectedAmount, string? returnReason = null);
        Task AcceptCompanyPickupAsync(int jobId);
        Task<DeliveryJob?> GetJobByTokenAsync(string token);
        Task AssignOrderToCompanyAsync(int orderId, int companyId, decimal deliveryFee);

        // Drivers
        Task<DeliveryDriver> RegisterDriverAsync(
            int userId,
            int? companyId,
            string? vehicleType,
            string? vehiclePlateNumber,
            string? licenseNumber,
            string? nationalId = null,
            string? heavyLicenseCategory = null,
            string? licenseDocUrl = null,
            string? registrationDocUrl = null,
            string? vehiclePhotoUrl = null,
            string? cliqAlias = null,
            int capacityTons = 15,
            string? coveredGovernorate = null);
        Task<DeliveryDriver?> GetDriverByUserIdAsync(int userId);
        Task<DeliveryDriver?> GetDriverByIdAsync(int driverId);
        Task<IEnumerable<DeliveryDriver>> GetDriversAsync();
        Task<Bolcko.Domain.Common.IPagedList<DeliveryDriver>> GetPagedDriversAsync(int pageIndex, int pageSize);
        Task ApproveDriverAsync(int driverId);
        Task UpdateDriverAvailabilityAsync(int driverId, bool isAvailable);

        // Jobs
        Task<DeliveryJob> CreateJobForOrderAsync(int orderId, decimal deliveryFee);
        Task<DeliveryJob?> GetJobByIdAsync(int jobId);
        Task<DeliveryJob?> GetJobByOrderIdAsync(int orderId);
        Task<IEnumerable<DeliveryJob>> GetAvailableJobsAsync();
        Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedAvailableJobsAsync(int pageIndex, int pageSize);
        Task<IEnumerable<DeliveryJob>> GetAllJobsAsync();
        Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedAllJobsAsync(int pageIndex, int pageSize, DeliveryJobStatus? statusFilter = null);
        Task<IEnumerable<DeliveryJob>> GetDriverJobsAsync(int driverId);
        Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedDriverJobsAsync(int driverId, int pageIndex, int pageSize);
        Task AssignJobToDriverAsync(int jobId, int driverId, decimal fee);
        Task UpdateJobStatusAsync(int jobId, DeliveryJobStatus status);
        Task SendDeliveryDocumentsToCompanyAsync(int jobId, string? overrideEmail = null, bool includePdf = true, bool includeExcel = true, string? customMessage = null);

        // Bids
        Task<DeliveryBid> PlaceBidAsync(int jobId, int driverId, decimal bidAmount);
        Task<IEnumerable<DeliveryBid>> GetBidsForJobAsync(int jobId);
        Task<IEnumerable<DeliveryBid>> GetDriverBidsAsync(int driverId);
        Task AcceptBidAsync(int bidId);

        // Ratings
        Task SubmitRatingAsync(int jobId, int customerId, int ratingValue, string? comment);
        Task<IEnumerable<DeliveryRating>> GetDriverRatingsAsync(int driverId);

        // LOG-06 Instant CliQ Carrier Wallet Payout & Escrow Release
        Task<Bolcko.Domain.Entities.Delivery.DTOs.CarrierPayoutResultDto> ReleaseJobsiteOtpPayoutAsync(int jobId, string otpCode, string? receiverNotes = null);
        Task<bool> DisputeDeliveryJobAsync(int jobId, string disputeReason, string? photoEvidenceUrl = null);
        Task<Bolcko.Domain.Entities.Financing.CarrierPayoutTransaction?> GetPayoutTransactionByJobIdAsync(int jobId);
    }
}
