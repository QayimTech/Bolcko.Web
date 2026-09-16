using Blocko.Services.Interfaces.Financing;
using Bolcko.Domain.Entities.Financing;
using Bolcko.Domain.Entities.Financing.DTOs;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blocko.Services.Implementations.Financing
{
    public class FinancingService : IFinancingService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<FinancingService> _logger;

        public FinancingService(IUnitOfWork uow, ILogger<FinancingService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<FinancingTenderDto> CreateTenderFromBOQAsync(CreateFinancingTenderRequestDto request, int? userId = null)
        {
            if (request == null || !request.Items.Any())
            {
                throw new ArgumentException("Cannot create a financing tender with empty items.");
            }

            decimal baseCost = request.Items.Sum(i => i.SubtotalJod);
            decimal markupRate = 0.06m; // 6% Murabaha markup
            decimal totalPayable = Math.Round(baseCost * (1.0m + markupRate), 2);
            decimal agencyFeeRate = 0.015m; // 1.5% Block-O agency fee
            decimal agencyFeeAmount = Math.Round(baseCost * agencyFeeRate, 2);
            decimal investorYield = Math.Round(totalPayable - baseCost - agencyFeeAmount, 2);

            var trackingCode = $"MRB-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var tender = new FinancingTender
            {
                ContractorId = userId,
                ContractorName = request.FullName,
                ContractorPhone = request.Phone,
                ContractorCompany = request.Company,
                TrackingCode = trackingCode,
                ProjectTitle = request.ProjectTitle,
                ProjectCity = request.ProjectCity,
                ProjectAddress = request.ProjectAddress,
                BuildingPermitNumber = request.BuildingPermitNumber,
                TargetLatitude = request.Latitude,
                TargetLongitude = request.Longitude,
                BaseMaterialCost = baseCost,
                ContractorMarkupRate = markupRate,
                TotalPayableAmount = totalPayable,
                PlatformAgencyFeeRate = agencyFeeRate,
                PlatformAgencyFeeAmount = agencyFeeAmount,
                InvestorNetYieldAmount = investorYield,
                TenureDays = request.TenureDays,
                DueDate = DateTime.UtcNow.AddDays(request.TenureDays),
                Status = FinancingTenderStatus.OpenForBidding,
                CreatedAt = DateTime.UtcNow,
                IsCollateralVerified = true,
                ContractorTrustScore = 96.5
            };

            foreach (var it in request.Items)
            {
                tender.Items.Add(new FinancingTenderItem
                {
                    MaterialCategory = it.MaterialCategory,
                    MaterialName = it.MaterialName,
                    Quantity = it.Quantity,
                    Unit = it.Unit,
                    UnitPriceJod = it.UnitPriceJod,
                    SubtotalJod = it.SubtotalJod
                });
            }

            await _uow.FinancingTenders.AddAsync(tender);
            await _uow.CompleteAsync();

            _logger.LogInformation("Financing tender created: {Code}, Amount: {Amount} JOD", trackingCode, totalPayable);

            return MapToDto(tender);
        }

        public async Task<IEnumerable<FinancingTenderDto>> GetOpenTendersAsync()
        {
            var tenders = await _uow.FinancingTenders.GetOpenTendersAsync();
            return tenders.Select(MapToDto).ToList();
        }

        public async Task<FinancingTenderDto?> GetTenderByIdAsync(int id)
        {
            var tender = await _uow.FinancingTenders.GetTenderWithDetailsAsync(id);
            return tender != null ? MapToDto(tender) : null;
        }

        public async Task<FinancingTenderDto?> GetTenderByTrackingCodeAsync(string trackingCode)
        {
            var tender = await _uow.FinancingTenders.GetTenderByTrackingCodeAsync(trackingCode);
            return tender != null ? MapToDto(tender) : null;
        }

        public async Task<bool> FundTenderAsync(FundTenderRequestDto request, int? funderId = null)
        {
            var tender = await _uow.FinancingTenders.GetTenderWithDetailsAsync(request.TenderId);
            if (tender == null || tender.Status != FinancingTenderStatus.OpenForBidding)
            {
                return false;
            }

            tender.FunderInvestorId = funderId;
            tender.FunderName = request.FunderName;
            tender.FunderPhone = request.FunderPhone;
            tender.FundedAt = DateTime.UtcNow;
            tender.Status = FinancingTenderStatus.Funded;
            tender.WakalaContractPdfUrl = $"/contracts/wakala-{tender.TrackingCode}.pdf";

            _uow.FinancingTenders.Update(tender);
            await _uow.CompleteAsync();

            _logger.LogInformation("Tender {Code} funded successfully by {Funder}", tender.TrackingCode, request.FunderName);
            return true;
        }

        public async Task<JobsitePodDto> SubmitJobsitePodAsync(SubmitJobsitePodRequestDto request)
        {
            var tender = await _uow.FinancingTenders.GetTenderWithDetailsAsync(request.TenderId);
            if (tender == null)
            {
                throw new ArgumentException("Tender not found.");
            }

            double dist = CalculateDistanceMeters(
                request.DriverLatitude, request.DriverLongitude,
                tender.TargetLatitude > 0 ? tender.TargetLatitude : 31.9539,
                tender.TargetLongitude > 0 ? tender.TargetLongitude : 35.9106);

            bool withinGeoFence = dist <= 150.0;

            var pod = new JobsiteProofOfDelivery
            {
                FinancingTenderId = tender.Id,
                DriverName = request.DriverName,
                DriverPhone = request.DriverPhone,
                VehiclePlateNumber = request.VehiclePlateNumber,
                DriverLatitude = request.DriverLatitude,
                DriverLongitude = request.DriverLongitude,
                TargetJobsiteLatitude = tender.TargetLatitude,
                TargetJobsiteLongitude = tender.TargetLongitude,
                DistanceVarianceMeters = Math.Round(dist, 1),
                IsWithinGeoFence = withinGeoFence,
                PhotoEvidenceUrl = !string.IsNullOrEmpty(request.PhotoUrl) ? request.PhotoUrl : "/images/pod-verified.jpg",
                DeliveredAt = DateTime.UtcNow,
                ContractorConfirmed = true,
                ContractorSignOffTime = DateTime.UtcNow
            };

            await _uow.JobsiteDeliveries.AddAsync(pod);

            tender.Status = FinancingTenderStatus.Delivered;
            tender.DeliveredAt = DateTime.UtcNow;
            _uow.FinancingTenders.Update(tender);

            await _uow.CompleteAsync();

            return new JobsitePodDto
            {
                Id = pod.Id,
                FinancingTenderId = pod.FinancingTenderId,
                DriverName = pod.DriverName,
                DriverPhone = pod.DriverPhone,
                VehiclePlateNumber = pod.VehiclePlateNumber,
                DistanceVarianceMeters = pod.DistanceVarianceMeters,
                IsWithinGeoFence = pod.IsWithinGeoFence,
                PhotoEvidenceUrl = pod.PhotoEvidenceUrl,
                DeliveredAt = pod.DeliveredAt,
                ContractorConfirmed = pod.ContractorConfirmed
            };
        }

        public async Task<bool> SettleTenderAsync(int tenderId)
        {
            var tender = await _uow.FinancingTenders.GetByIdAsync(tenderId);
            if (tender == null) return false;

            tender.Status = FinancingTenderStatus.Settled;
            tender.SettledAt = DateTime.UtcNow;
            _uow.FinancingTenders.Update(tender);
            await _uow.CompleteAsync();

            _logger.LogInformation("Tender {Code} settled. Platform Fee: {Fee} JOD, Investor Return: {Ret} JOD",
                tender.TrackingCode, tender.PlatformAgencyFeeAmount, tender.BaseMaterialCost + tender.InvestorNetYieldAmount);

            return true;
        }

        public double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Radius of Earth in meters
            double dLat = (lat2 - lat1) * (Math.PI / 180.0);
            double dLon = (lon2 - lon1) * (Math.PI / 180.0);

            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return R * c;
        }

        private static FinancingTenderDto MapToDto(FinancingTender t)
        {
            decimal annualized = t.BaseMaterialCost > 0 && t.TenureDays > 0
                ? Math.Round((t.InvestorNetYieldAmount / t.BaseMaterialCost) * (365.0m / t.TenureDays) * 100m, 2)
                : 0m;

            return new FinancingTenderDto
            {
                Id = t.Id,
                TrackingCode = t.TrackingCode,
                ProjectTitle = t.ProjectTitle,
                ProjectCity = t.ProjectCity,
                ProjectAddress = t.ProjectAddress,
                BuildingPermitNumber = t.BuildingPermitNumber,
                ContractorName = t.ContractorName,
                ContractorPhone = t.ContractorPhone,
                ContractorCompany = t.ContractorCompany,
                ContractorTrustScore = t.ContractorTrustScore,
                BaseMaterialCost = t.BaseMaterialCost,
                ContractorMarkupRate = t.ContractorMarkupRate,
                TotalPayableAmount = t.TotalPayableAmount,
                PlatformAgencyFeeAmount = t.PlatformAgencyFeeAmount,
                InvestorNetYieldAmount = t.InvestorNetYieldAmount,
                AnnualizedYieldPercentage = annualized,
                TenureDays = t.TenureDays,
                DueDate = t.DueDate,
                Status = t.Status,
                StatusNameAr = GetStatusArabicName(t.Status),
                FunderName = t.FunderName,
                CreatedAt = t.CreatedAt,
                FundedAt = t.FundedAt,
                DeliveredAt = t.DeliveredAt,
                Items = t.Items.Select(i => new FinancingTenderItemDto
                {
                    Id = i.Id,
                    MaterialCategory = i.MaterialCategory,
                    MaterialName = i.MaterialName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPriceJod = i.UnitPriceJod,
                    SubtotalJod = i.SubtotalJod
                }).ToList(),
                Deliveries = t.Deliveries.Select(d => new JobsitePodDto
                {
                    Id = d.Id,
                    FinancingTenderId = d.FinancingTenderId,
                    DriverName = d.DriverName,
                    DriverPhone = d.DriverPhone,
                    VehiclePlateNumber = d.VehiclePlateNumber,
                    DistanceVarianceMeters = d.DistanceVarianceMeters,
                    IsWithinGeoFence = d.IsWithinGeoFence,
                    PhotoEvidenceUrl = d.PhotoEvidenceUrl,
                    DeliveredAt = d.DeliveredAt,
                    ContractorConfirmed = d.ContractorConfirmed
                }).ToList()
            };
        }

        private static string GetStatusArabicName(FinancingTenderStatus status) => status switch
        {
            FinancingTenderStatus.Draft => "مسودة",
            FinancingTenderStatus.OpenForBidding => "متاح للتمويل",
            FinancingTenderStatus.Funded => "تم التمويل وشراء المواد",
            FinancingTenderStatus.GoodsPurchased => "تم إصدار أمر الشراء",
            FinancingTenderStatus.Dispatched => "جاري الشحن والتوصيل",
            FinancingTenderStatus.Delivered => "تم التسليم بالموقع والتحقق",
            FinancingTenderStatus.Settled => "تمت التسوية وسداد الأرباح",
            FinancingTenderStatus.Defaulted => "متعثر",
            FinancingTenderStatus.Cancelled => "ملغي",
            _ => status.ToString()
        };
    }
}