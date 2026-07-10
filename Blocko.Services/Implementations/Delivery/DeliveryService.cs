using Blocko.Services.Interfaces.Delivery;
using Blocko.Services.Interfaces.Notifications;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Enums;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Services.Implementations.Delivery
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IDeliveryDocumentService _deliveryDocumentService;
        private readonly Blocko.Services.Interfaces.User.IEmailSender _emailSender;

        public DeliveryService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IDeliveryDocumentService deliveryDocumentService,
            Blocko.Services.Interfaces.User.IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _deliveryDocumentService = deliveryDocumentService;
            _emailSender = emailSender;
        }


        #region Companies
        public async Task<DeliveryCompany> CreateCompanyAsync(string name, string? email, string? phoneNumber, string? commercialRegister, decimal baseRate)
        {
            var company = new DeliveryCompany
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber,
                CommercialRegister = commercialRegister,
                BaseDeliveryRate = baseRate,
                IsActive = true
            };

            await _unitOfWork.DeliveryCompanies.AddAsync(company);
            await _unitOfWork.CompleteAsync();
            return company;
        }

        public async Task<IEnumerable<DeliveryCompany>> GetAllCompaniesAsync()
        {
            return await _unitOfWork.DeliveryCompanies.GetAllAsync();
        }

        public async Task<DeliveryCompany?> GetCompanyByIdAsync(int companyId)
        {
            return await _unitOfWork.DeliveryCompanies.GetByIdAsync(companyId);
        }
        #endregion

        #region Drivers
        public async Task<DeliveryDriver> RegisterDriverAsync(int userId, int? companyId, string? vehicleType, string? vehiclePlateNumber, string? licenseNumber)
        {
            var driver = new DeliveryDriver
            {
                UserId = userId,
                DeliveryCompanyId = companyId,
                VehicleType = vehicleType,
                VehiclePlateNumber = vehiclePlateNumber,
                LicenseNumber = licenseNumber,
                IsAvailable = true,
                IsApproved = false,
                AverageRating = 0.0m,
                TotalRatings = 0
            };

            await _unitOfWork.DeliveryDrivers.AddAsync(driver);
            await _unitOfWork.CompleteAsync();
            return driver;
        }

        public async Task<DeliveryDriver?> GetDriverByUserIdAsync(int userId)
        {
            return await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<DeliveryDriver?> GetDriverByIdAsync(int driverId)
        {
            return await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.Id == driverId);
        }

        public async Task<IEnumerable<DeliveryDriver>> GetDriversAsync()
        {
            return await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Include(d => d.Company)
                .ToListAsync();
        }

        public async Task ApproveDriverAsync(int driverId)
        {
            var driver = await _unitOfWork.DeliveryDrivers.GetByIdAsync(driverId);
            if (driver != null)
            {
                driver.IsApproved = true;
                _unitOfWork.DeliveryDrivers.Update(driver);
                await _unitOfWork.CompleteAsync();

                // Notify driver
                await _notificationService.SendNotificationToUserAsync(driver.UserId, "تفعيل الحساب", "تمت الموافقة على حساب السائق الخاص بك وتفعيله بنجاح!");
            }
        }

        public async Task UpdateDriverAvailabilityAsync(int driverId, bool isAvailable)
        {
            var driver = await _unitOfWork.DeliveryDrivers.GetByIdAsync(driverId);
            if (driver != null)
            {
                driver.IsAvailable = isAvailable;
                _unitOfWork.DeliveryDrivers.Update(driver);
                await _unitOfWork.CompleteAsync();
            }
        }
        #endregion

        #region Jobs
        public async Task<DeliveryJob> CreateJobForOrderAsync(int orderId, decimal deliveryFee)
        {
            var order = await _unitOfWork.Orders.GetAllAsQueryable()
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new ArgumentException("Order not found");

            var job = new DeliveryJob
            {
                OrderId = orderId,
                DeliveryFee = deliveryFee,
                Status = DeliveryJobStatus.Available,
                PickupLocation = "مخازن بولكو الرئيسية", // Default pickup address or warehouse location
                DropoffLocation = $"{order.ShippingAddress.City}, {order.ShippingAddress.AddressLine1}"
            };

            await _unitOfWork.DeliveryJobs.AddAsync(job);
            await _unitOfWork.CompleteAsync();

            // Notify all drivers about the new job
            await _notificationService.SendNotificationToRoleAsync("DeliveryDriver", "طلب توصيل جديد متاح", $"طلب توصيل جديد متوفر بقيمة {deliveryFee} لـ {order.ShippingAddress.City}");

            return job;
        }

        public async Task<DeliveryJob?> GetJobByIdAsync(int jobId)
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .ThenInclude(o => o.ShippingAddress)
                .Include(j => j.Driver)
                .ThenInclude(d => d.User)
                .Include(j => j.Bids)
                .ThenInclude(b => b.Driver)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == jobId);
        }

        public async Task<DeliveryJob?> GetJobByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Driver)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.OrderId == orderId);
        }

        public async Task<IEnumerable<DeliveryJob>> GetAvailableJobsAsync()
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .ThenInclude(o => o.ShippingAddress)
                .Where(j => j.Status == DeliveryJobStatus.Available)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeliveryJob>> GetAllJobsAsync()
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Driver)
                .ThenInclude(d => d.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeliveryJob>> GetDriverJobsAsync(int driverId)
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .ThenInclude(o => o.ShippingAddress)
                .Where(j => j.DriverId == driverId)
                .ToListAsync();
        }

        public async Task AssignJobToDriverAsync(int jobId, int driverId, decimal fee)
        {
            var job = await _unitOfWork.DeliveryJobs.GetByIdAsync(jobId);
            var driver = await _unitOfWork.DeliveryDrivers.GetByIdAsync(driverId);

            if (job == null || driver == null)
                throw new ArgumentException("Job or Driver not found");

            job.DriverId = driverId;
            job.DeliveryFee = fee;
            job.Status = DeliveryJobStatus.Assigned;
            job.AssignedAt = DateTime.UtcNow;

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            // Notify driver
            await _notificationService.SendNotificationToUserAsync(driver.UserId, "تم تعيين مهمة توصيل لك", $"تم تعيين الطلب رقم {job.OrderId} لك للتوصيل.");
            
            // Notify customer
            var order = await _unitOfWork.Orders.GetByIdAsync(job.OrderId);
            if (order != null)
            {
                await _notificationService.SendNotificationToUserAsync(order.UserId, "جاري توصيل طلبك", $"تم تعيين المندوب لتوصيل طلبك رقم {order.OrderNumber}.");
            }

            // Automatically send documents to the company (swallow any exceptions so assignment succeeds)
            try
            {
                await SendDeliveryDocumentsToCompanyAsync(jobId);
            }
            catch (Exception ex)
            {
                // Swallowed
            }
        }

        public async Task UpdateJobStatusAsync(int jobId, DeliveryJobStatus status)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                throw new ArgumentException("Job not found");

            job.Status = status;
            if (status == DeliveryJobStatus.PickedUp)
            {
                job.PickedUpAt = DateTime.UtcNow;
            }
            else if (status == DeliveryJobStatus.Delivered)
            {
                job.DeliveredAt = DateTime.UtcNow;
                
                // Update Order Status also to Delivered
                if (job.Order != null)
                {
                    job.Order.Status = OrderStatus.Delivered;
                    _unitOfWork.Orders.Update(job.Order);
                }
            }

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            // Notify customer
            if (job.Order != null)
            {
                string message = status switch
                {
                    DeliveryJobStatus.PickedUp => "طلبك بالطريق مع المندوب الآن.",
                    DeliveryJobStatus.InTransit => "المندوب يقترب من موقعك الآن.",
                    DeliveryJobStatus.Delivered => "تم تسليم طلبك بنجاح. شكراً لك!",
                    DeliveryJobStatus.Cancelled => "تم إلغاء مهمة التوصيل.",
                    _ => $"تم تحديث حالة التوصيل إلى: {status}"
                };

                await _notificationService.SendNotificationToUserAsync(job.Order.UserId, "تحديث حالة التوصيل", message);
            }
        }

        public async Task SendDeliveryDocumentsToCompanyAsync(int jobId)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .ThenInclude(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(j => j.Order)
                .ThenInclude(o => o.ShippingAddress)
                .Include(j => j.Order)
                .ThenInclude(o => o.User)
                .Include(j => j.Driver)
                .ThenInclude(d => d.Company)
                .Include(j => j.Driver)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                throw new ArgumentException("Job not found");

            var driver = job.Driver;
            if (driver == null || driver.DeliveryCompanyId == null)
            {
                // Driver is a freelancer or not assigned, no company to notify
                return;
            }

            var company = driver.Company;
            if (company == null || string.IsNullOrWhiteSpace(company.Email))
            {
                // Company has no email configured
                return;
            }

            // Generate attachments
            byte[] excelBytes = _deliveryDocumentService.GenerateExcelSheet(job);
            byte[] pdfBytes = _deliveryDocumentService.GeneratePdfDocument(job);

            // Construct email
            string subject = $"📦 تفاصيل طلب التوصيل رقم {job.OrderId} - بولكو";
            string htmlMessage = $@"
<div dir='rtl' style='font-family: Arial, sans-serif; line-height: 1.6; color: #1E293B;'>
    <div style='background-color: #E8A020; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
        <h1 style='color: #FFFFFF; margin: 0;'>بولكو للوازم البناء والتوصيل</h1>
    </div>
    <div style='padding: 20px; border: 1px solid #E2E8F0; border-top: none; border-radius: 0 0 8px 8px;'>
        <p>مرحباً <strong>{company.Name}</strong>،</p>
        <p>تم تعيين مهمة توصيل جديدة للمندوب التابع لكم <strong>{driver.User?.FirstName} {driver.User?.LastName}</strong>.</p>
        
        <h3 style='border-bottom: 2px solid #E8A020; padding-bottom: 8px;'>تفاصيل المهمة:</h3>
        <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
            <tr>
                <td style='padding: 8px; border: 1px solid #E2E8F0; background-color: #F8FAFC; width: 30%;'><strong>رقم المهمة:</strong></td>
                <td style='padding: 8px; border: 1px solid #E2E8F0;'>#{job.Id}</td>
            </tr>
            <tr>
                <td style='padding: 8px; border: 1px solid #E2E8F0; background-color: #F8FAFC;'><strong>رقم الطلب:</strong></td>
                <td style='padding: 8px; border: 1px solid #E2E8F0;'>{job.Order?.OrderNumber}</td>
            </tr>
            <tr>
                <td style='padding: 8px; border: 1px solid #E2E8F0; background-color: #F8FAFC;'><strong>موقع الاستلام:</strong></td>
                <td style='padding: 8px; border: 1px solid #E2E8F0;'>{job.PickupLocation}</td>
            </tr>
            <tr>
                <td style='padding: 8px; border: 1px solid #E2E8F0; background-color: #F8FAFC;'><strong>موقع التسليم:</strong></td>
                <td style='padding: 8px; border: 1px solid #E2E8F0;'>{job.DropoffLocation}</td>
            </tr>
            <tr>
                <td style='padding: 8px; border: 1px solid #E2E8F0; background-color: #F8FAFC;'><strong>قيمة التوصيل:</strong></td>
                <td style='padding: 8px; border: 1px solid #E2E8F0; color: #E8A020; font-weight: bold;'>{job.DeliveryFee:N2} د.أ</td>
            </tr>
        </table>

        <p style='background-color: #FEF08A; padding: 12px; border-radius: 6px; font-weight: bold; text-align: center; color: #854D0E;'>
            يرجى الاطلاع على ملف الـ PDF وملف الـ Excel المرفقين للحصول على التفاصيل الكاملة للمستلم وقائمة المواد المطلوب تسليمها.
        </p>

        <p style='margin-top: 30px; font-size: 12px; color: #94A3B8; text-align: center;'>
            هذا البريد تم إنشاؤه تلقائياً من نظام بولكو - يرجى عدم الرد.
        </p>
    </div>
</div>";

            var attachments = new List<(byte[] content, string fileName, string contentType)>
            {
                (pdfBytes, $"DeliveryJob_{job.Id}.pdf", "application/pdf"),
                (excelBytes, $"DeliveryJob_{job.Id}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            };

            await _emailSender.SendEmailAsync(company.Email, subject, htmlMessage, attachments);
        }
        #endregion

        #region Bids
        public async Task<DeliveryBid> PlaceBidAsync(int jobId, int driverId, decimal bidAmount)
        {
            var bid = new DeliveryBid
            {
                DeliveryJobId = jobId,
                DriverId = driverId,
                BidAmount = bidAmount,
                Status = DeliveryBidStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliveryBids.AddAsync(bid);
            await _unitOfWork.CompleteAsync();

            // Notify Admin
            await _notificationService.SendNotificationToRoleAsync("Admin", "عرض سعر جديد", $"قدم المندوب عرض سعر بقيمة {bidAmount} للطلب رقم {jobId}.");

            return bid;
        }

        public async Task<IEnumerable<DeliveryBid>> GetBidsForJobAsync(int jobId)
        {
            return await _unitOfWork.DeliveryBids.GetAllAsQueryable()
                .Include(b => b.Driver)
                .ThenInclude(d => d.User)
                .Where(b => b.DeliveryJobId == jobId)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeliveryBid>> GetDriverBidsAsync(int driverId)
        {
            return await _unitOfWork.DeliveryBids.GetAllAsQueryable()
                .Include(b => b.DeliveryJob)
                .ThenInclude(j => j.Order)
                .Where(b => b.DriverId == driverId)
                .ToListAsync();
        }

        public async Task AcceptBidAsync(int bidId)
        {
            var bid = await _unitOfWork.DeliveryBids.GetAllAsQueryable()
                .Include(b => b.DeliveryJob)
                .Include(b => b.Driver)
                .FirstOrDefaultAsync(b => b.Id == bidId);

            if (bid == null)
                throw new ArgumentException("Bid not found");

            // Reject all other bids for this job
            var otherBids = await _unitOfWork.DeliveryBids.GetAllAsQueryable()
                .Where(b => b.DeliveryJobId == bid.DeliveryJobId && b.Id != bidId)
                .ToListAsync();

            foreach (var otherBid in otherBids)
            {
                otherBid.Status = DeliveryBidStatus.Rejected;
                _unitOfWork.DeliveryBids.Update(otherBid);
            }

            bid.Status = DeliveryBidStatus.Accepted;
            _unitOfWork.DeliveryBids.Update(bid);

            // Assign Job to this Driver
            await AssignJobToDriverAsync(bid.DeliveryJobId, bid.DriverId, bid.BidAmount);
        }
        #endregion

        #region Ratings
        public async Task SubmitRatingAsync(int jobId, int customerId, int ratingValue, string? comment)
        {
            var job = await _unitOfWork.DeliveryJobs.GetByIdAsync(jobId);
            if (job == null)
                throw new ArgumentException("Job not found");

            if (job.DriverId == null)
                throw new InvalidOperationException("Cannot rate a job without assigned driver");

            var rating = new DeliveryRating
            {
                DeliveryJobId = jobId,
                DriverId = job.DriverId.Value,
                CustomerId = customerId,
                RatingValue = ratingValue,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliveryRatings.AddAsync(rating);
            await _unitOfWork.CompleteAsync();

            // Recalculate average rating for driver
            var driver = await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.Ratings)
                .FirstOrDefaultAsync(d => d.Id == job.DriverId.Value);

            if (driver != null)
            {
                var ratings = driver.Ratings.ToList();
                driver.TotalRatings = ratings.Count;
                driver.AverageRating = (decimal)ratings.Average(r => r.RatingValue);
                _unitOfWork.DeliveryDrivers.Update(driver);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<IEnumerable<DeliveryRating>> GetDriverRatingsAsync(int driverId)
        {
            return await _unitOfWork.DeliveryRatings.GetAllAsQueryable()
                .Include(r => r.Customer)
                .Where(r => r.DriverId == driverId)
                .ToListAsync();
        }
        #endregion

        #region Paged Methods
        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryCompany>> GetPagedCompaniesAsync(int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryCompanies.GetPagedAsync(
                pageIndex, 
                pageSize, 
                orderBy: q => q.OrderByDescending(c => c.Id));
        }

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryDriver>> GetPagedDriversAsync(int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryDrivers.GetPagedAsync(
                pageIndex, 
                pageSize, 
                orderBy: q => q.OrderByDescending(d => d.Id),
                includes: new System.Linq.Expressions.Expression<Func<DeliveryDriver, object>>[] { d => d.User!, d => d.Company! });
        }

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedAvailableJobsAsync(int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryJobs.GetPagedAsync(
                pageIndex, 
                pageSize, 
                predicate: j => j.Status == DeliveryJobStatus.Available,
                orderBy: q => q.OrderByDescending(j => j.Id),
                includes: new System.Linq.Expressions.Expression<Func<DeliveryJob, object>>[] { j => j.Order!, j => j.Order!.ShippingAddress! });
        }

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedAllJobsAsync(int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryJobs.GetPagedAsync(
                pageIndex, 
                pageSize, 
                orderBy: q => q.OrderByDescending(j => j.Id),
                includes: new System.Linq.Expressions.Expression<Func<DeliveryJob, object>>[] { j => j.Order!, j => j.Driver!, j => j.Driver!.User! });
        }

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedDriverJobsAsync(int driverId, int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryJobs.GetPagedAsync(
                pageIndex, 
                pageSize, 
                predicate: j => j.DriverId == driverId,
                orderBy: q => q.OrderByDescending(j => j.Id),
                includes: new System.Linq.Expressions.Expression<Func<DeliveryJob, object>>[] { j => j.Order!, j => j.Order!.ShippingAddress! });
        }
        #endregion
    }
}
