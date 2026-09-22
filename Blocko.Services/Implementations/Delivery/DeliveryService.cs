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
        public async Task<DeliveryCompany> CreateCompanyAsync(
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
            bool isApproved = false)
        {
            var company = new DeliveryCompany
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber,
                CommercialRegister = commercialRegister,
                BaseDeliveryRate = baseRate,
                ManagerUserId = managerUserId,
                IsActive = true,
                TaxId = taxId,
                TransportCommissionLicense = transportCommissionLicense,
                CommercialRegisterDocUrl = commercialRegisterDocUrl,
                TransportLicenseDocUrl = transportLicenseDocUrl,
                CliqAlias = cliqAlias,
                TotalTrucksCount = totalTrucksCount,
                IsApproved = isApproved
            };

            await _unitOfWork.DeliveryCompanies.AddAsync(company);
            await _unitOfWork.CompleteAsync();
            return company;
        }

        public async Task<IEnumerable<DeliveryCompany>> GetAllCompaniesAsync()
        {
            return await _unitOfWork.DeliveryCompanies.GetAllAsQueryable()
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<DeliveryCompany>> GetActiveCompaniesAsync()
        {
            return await _unitOfWork.DeliveryCompanies.GetAllAsQueryable()
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task DeleteCompanyAsync(int companyId)
        {
            var company = await _unitOfWork.DeliveryCompanies.GetByIdAsync(companyId);
            if (company != null)
            {
                company.IsActive = false;
                company.ManagerUserId = null;
                _unitOfWork.DeliveryCompanies.Update(company);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<DeliveryCompany?> GetCompanyByIdAsync(int companyId)
        {
            return await _unitOfWork.DeliveryCompanies.GetByIdAsync(companyId);
        }

        public async Task ToggleCompanyStatusAsync(int companyId)
        {
            var company = await _unitOfWork.DeliveryCompanies.GetByIdAsync(companyId);
            if (company != null)
            {
                company.IsActive = !company.IsActive;
                _unitOfWork.DeliveryCompanies.Update(company);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<DeliveryCompany?> GetCompanyByManagerUserIdAsync(string managerUserId)
        {
            return await _unitOfWork.DeliveryCompanies.GetAllAsQueryable()
                .Include(c => c.Drivers)
                .FirstOrDefaultAsync(c => c.ManagerUserId == managerUserId);
        }

        public async Task<IEnumerable<DeliveryJob>> GetCompanyJobsAsync(int companyId)
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Order.User)
                .Include(j => j.Order.ShippingAddress)
                .Where(j => j.DeliveryCompanyId == companyId)
                .ToListAsync();
        }

        public async Task UpdateCompanyJobCollectedAmountAsync(int jobId, decimal collectedAmount, string? returnReason = null)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job != null)
            {
                job.CollectedAmount = collectedAmount;
                job.ReturnReason = returnReason;

                if (collectedAmount == 0 && !string.IsNullOrEmpty(returnReason))
                {
                    job.Status = DeliveryJobStatus.Returned;
                    if (job.Order != null)
                    {
                        job.Order.Status = OrderStatus.Cancelled;
                        _unitOfWork.Orders.Update(job.Order);
                    }
                }
                else
                {
                    job.Status = DeliveryJobStatus.Delivered;
                    job.DeliveredAt = DateTime.UtcNow;
                    if (job.Order != null)
                    {
                        job.Order.Status = OrderStatus.Delivered;
                        _unitOfWork.Orders.Update(job.Order);
                    }
                }

                _unitOfWork.DeliveryJobs.Update(job);
                await _unitOfWork.CompleteAsync();

                // Send notification to customer
                if (job.Order != null)
                {
                    string title = job.Status == DeliveryJobStatus.Delivered ? "تم تسليم طلبك بنجاح 💚" : "تحديث حالة الطلب";
                    string msg = job.Status == DeliveryJobStatus.Delivered 
                        ? $"تم تسليم الطلب رقم {job.Order.OrderNumber} بنجاح ومزامنته. يسعدنا تقييمك للخدمة!" 
                        : $"تعذر تسليم الطلب رقم {job.Order.OrderNumber}. السبب: {returnReason ?? "مرتجع"}";

                    await _notificationService.SendNotificationToUserAsync(job.Order.UserId, title, msg, $"/Shop/Account/OrderDetails/{job.OrderId}");
                }
            }
        }
        public async Task AcceptCompanyPickupAsync(int jobId)
        {
            var job = await _unitOfWork.DeliveryJobs.GetByIdAsync(jobId);
            if (job != null)
            {
                job.Status = DeliveryJobStatus.PickedUp;
                job.PickedUpAt = DateTime.UtcNow;
                job.DeliveryToken = System.Guid.NewGuid().ToString("N");
                
                _unitOfWork.DeliveryJobs.Update(job);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<DeliveryJob?> GetJobByTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Order.User)
                .Include(j => j.Order.ShippingAddress)
                .FirstOrDefaultAsync(j => j.DeliveryToken == token);
        }

        public async Task AssignOrderToCompanyAsync(int orderId, int companyId, decimal deliveryFee)
        {
            var order = await _unitOfWork.Orders.GetAllAsQueryable()
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId);
                
            if (order == null) throw new System.Exception("الطلب غير موجود");

            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .FirstOrDefaultAsync(j => j.OrderId == orderId);

            if (job != null)
            {
                job.DeliveryCompanyId = companyId;
                job.DeliveryFee = deliveryFee;
                job.Status = DeliveryJobStatus.Assigned;
                job.AssignedAt = DateTime.UtcNow;
                
                _unitOfWork.DeliveryJobs.Update(job);
            }
            else
            {
                job = new DeliveryJob
                {
                    OrderId = orderId,
                    DeliveryCompanyId = companyId,
                    DeliveryFee = deliveryFee,
                    Status = DeliveryJobStatus.Assigned,
                    AssignedAt = DateTime.UtcNow,
                    PickupLocation = "مستودع بلوكو الرئيسي",
                    DropoffLocation = $"{order.ShippingAddress?.AddressLine1 ?? ""}, {order.ShippingAddress?.City ?? ""}"
                };
                
                await _unitOfWork.DeliveryJobs.AddAsync(job);
            }
            
            await _unitOfWork.CompleteAsync();
        }
        #endregion

        #region Drivers
        public async Task<DeliveryDriver> RegisterDriverAsync(
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
            string? coveredGovernorate = null)
        {
            var driver = new DeliveryDriver
            {
                UserId = userId,
                DeliveryCompanyId = companyId,
                VehicleType = vehicleType,
                VehiclePlateNumber = vehiclePlateNumber,
                LicenseNumber = licenseNumber,
                NationalId = nationalId,
                HeavyLicenseCategory = heavyLicenseCategory ?? "الفئة السادسة - قاطرة ومقطورة",
                LicenseDocUrl = licenseDocUrl,
                RegistrationDocUrl = registrationDocUrl,
                VehiclePhotoUrl = vehiclePhotoUrl,
                CliqAlias = cliqAlias,
                CapacityTons = capacityTons,
                CoveredGovernorate = coveredGovernorate ?? "كافة محافظات المملكة",
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
                .Include(d => d.DeliveryCompany)
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<DeliveryDriver?> GetDriverByIdAsync(int driverId)
        {
            return await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Include(d => d.DeliveryCompany)
                .FirstOrDefaultAsync(d => d.Id == driverId);
        }

        public async Task<IEnumerable<DeliveryDriver>> GetDriversAsync()
        {
            return await _unitOfWork.DeliveryDrivers.GetAllAsQueryable()
                .Include(d => d.User)
                .Include(d => d.DeliveryCompany)
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
                .Include(j => j.Driver!)
                .ThenInclude(d => d.User)
                .Include(j => j.Bids)
                .ThenInclude(b => b.Driver!)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == jobId);
        }

        public async Task<DeliveryJob?> GetJobByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Driver!)
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
                .Include(j => j.Driver!)
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
            if (string.IsNullOrEmpty(job.DeliveryToken))
            {
                job.DeliveryToken = System.Guid.NewGuid().ToString("N");
            }

            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            // Notify driver
            await _notificationService.SendNotificationToUserAsync(driver.UserId, "تم تعيين مهمة توصيل لك", $"تم تعيين الطلب رقم {job.OrderId} لك للتوصيل.", "/Delivery/Home");
            
            // Notify customer
            var order = await _unitOfWork.Orders.GetByIdAsync(job.OrderId);
            if (order != null)
            {
                await _notificationService.SendNotificationToUserAsync(order.UserId, "جاري توصيل طلبك", $"تم تعيين المندوب لتوصيل طلبك رقم {order.OrderNumber}.", $"/Shop/Account/OrderDetails/{order.Id}");
            }

            // Automatically send documents to the company (swallow any exceptions so assignment succeeds)
            try
            {
                await SendDeliveryDocumentsToCompanyAsync(jobId);
            }
            catch (Exception)
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
            if (status == DeliveryJobStatus.PickedUp || status == DeliveryJobStatus.InTransit)
            {
                job.PickedUpAt ??= DateTime.UtcNow;
                if (job.Order != null)
                {
                    job.Order.Status = OrderStatus.Shipped;
                    _unitOfWork.Orders.Update(job.Order);
                }
            }
            else if (status == DeliveryJobStatus.Delivered)
            {
                job.DeliveredAt = DateTime.UtcNow;
                if (job.Order != null)
                {
                    job.Order.Status = OrderStatus.Delivered;
                    _unitOfWork.Orders.Update(job.Order);
                }
                
                if (job.DriverId.HasValue)
                {
                    var driver = await _unitOfWork.DeliveryDrivers.GetByIdAsync(job.DriverId.Value);
                    if (driver != null)
                    {
                        driver.TotalDeliveredOrders++;
                        
                        // Recalculate Tier Level
                        if (driver.TotalDeliveredOrders >= 50 && driver.AverageRating >= 4.8m)
                        {
                            driver.TierLevel = "Gold";
                        }
                        else if (driver.TotalDeliveredOrders >= 20 && driver.AverageRating >= 4.5m)
                        {
                            driver.TierLevel = "Silver";
                        }
                        else
                        {
                            driver.TierLevel = "Bronze";
                        }

                        _unitOfWork.DeliveryDrivers.Update(driver);
                    }
                }
            }
            else if (status == DeliveryJobStatus.Returned || status == DeliveryJobStatus.Cancelled)
            {
                if (job.Order != null)
                {
                    job.Order.Status = OrderStatus.Cancelled;
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

                await _notificationService.SendNotificationToUserAsync(job.Order.UserId, "تحديث حالة التوصيل", message, $"/Shop/Account/OrderDetails/{job.OrderId}");
            }
        }

        public async Task SendDeliveryDocumentsToCompanyAsync(int jobId, string? overrideEmail = null, bool includePdf = true, bool includeExcel = true, string? customMessage = null)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .ThenInclude(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(j => j.Order)
                .ThenInclude(o => o.ShippingAddress)
                .Include(j => j.Order)
                .ThenInclude(o => o.User)
                .Include(j => j.Driver!)
                .ThenInclude(d => d.DeliveryCompany)
                .Include(j => j.Driver!)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                throw new ArgumentException("Job not found");

            var driver = job.Driver;
            if (driver == null)
                throw new ArgumentException("No driver assigned to this job");

            string? recipientEmail = overrideEmail;
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                recipientEmail = driver.DeliveryCompany?.Email;
            }

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new ArgumentException("البريد الإلكتروني للشركة غير مهيأ، يرجى إدخال البريد يدوياً.");
            }

            var companyName = driver.DeliveryCompany?.Name ?? "سائق مستقل";

            // Generate attachments
            var attachments = new List<(byte[] content, string fileName, string contentType)>();

            if (includeExcel)
            {
                byte[] excelBytes = _deliveryDocumentService.GenerateExcelSheet(job);
                attachments.Add((excelBytes, $"DeliveryJob_{job.Id}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
            }

            if (includePdf)
            {
                byte[] pdfBytes = _deliveryDocumentService.GeneratePdfDocument(job);
                attachments.Add((pdfBytes, $"DeliveryJob_{job.Id}.pdf", "application/pdf"));
            }

            // Construct email message with custom note
            string customNoteHtml = "";
            if (!string.IsNullOrWhiteSpace(customMessage))
            {
                customNoteHtml = $@"
        <div style='background-color: #EFF6FF; border-right: 4px solid #3B82F6; padding: 15px; border-radius: 6px; margin-bottom: 20px; text-align: right;'>
            <strong style='color: #1E40AF; display: block; margin-bottom: 5px;'>📝 ملاحظة من الإدارة:</strong>
            <p style='margin: 0; color: #1E3A8A;'>{System.Net.WebUtility.HtmlEncode(customMessage)}</p>
        </div>";
            }

            string attachmentsNote = "";
            if (includePdf && includeExcel)
                attachmentsNote = "يرجى الاطلاع على ملف الـ PDF وملف الـ Excel المرفقين.";
            else if (includePdf)
                attachmentsNote = "يرجى الاطلاع على ملف الـ PDF المرفق.";
            else if (includeExcel)
                attachmentsNote = "يرجى الاطلاع على ملف الـ Excel المرفق.";

            // Construct email
            string subject = $"📦 تفاصيل طلب التوصيل رقم {job.OrderId} - بولكو";
            string htmlMessage = $@"
<div dir='rtl' style='font-family: Arial, sans-serif; line-height: 1.6; color: #1E293B; text-align: right;'>
    <div style='background-color: #E8A020; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
        <h1 style='color: #FFFFFF; margin: 0;'>بولكو للوازم البناء والتوصيل</h1>
    </div>
    <div style='padding: 20px; border: 1px solid #E2E8F0; border-top: none; border-radius: 0 0 8px 8px;'>
        <p>مرحباً <strong>{companyName}</strong>،</p>
        <p>تم تعيين مهمة توصيل جديدة للمندوب <strong>{driver.User?.FirstName} {driver.User?.LastName}</strong>.</p>
        
        {customNoteHtml}

        <h3 style='border-bottom: 2px solid #E8A020; padding-bottom: 8px;'>تفاصيل المهمة:</h3>
        <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px; text-align: right;'>
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

        {(string.IsNullOrEmpty(attachmentsNote) ? "" : $@"
        <p style='background-color: #FEF08A; padding: 12px; border-radius: 6px; font-weight: bold; text-align: center; color: #854D0E; margin-bottom: 20px;'>
            {attachmentsNote} للحصول على التفاصيل الكاملة للمستلم وقائمة المواد المطلوب تسليمها.
        </p>")}

        <p style='margin-top: 30px; font-size: 12px; color: #94A3B8; text-align: center;'>
            هذا البريد تم إنشاؤه تلقائياً من نظام بولكو - يرجى عدم الرد.
        </p>
    </div>
</div>";

            await _emailSender.SendEmailAsync(recipientEmail, subject, htmlMessage, attachments);
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
            await _notificationService.SendNotificationToRoleAsync("Admin", "عرض سعر جديد", $"قدم المندوب عرض سعر بقيمة {bidAmount:N2} د.أ للطلب رقم {jobId}. اضغط للتفاصيل.", $"/Admin/DeliveryDispatch/JobDetails/{jobId}");

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

                // Recalculate Tier Level
                if (driver.TotalDeliveredOrders >= 50 && driver.AverageRating >= 4.8m)
                {
                    driver.TierLevel = "Gold";
                }
                else if (driver.TotalDeliveredOrders >= 20 && driver.AverageRating >= 4.5m)
                {
                    driver.TierLevel = "Silver";
                }
                else
                {
                    driver.TierLevel = "Bronze";
                }

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
                orderBy: q => q.OrderByDescending(c => c.Id),
                includes: new System.Linq.Expressions.Expression<System.Func<DeliveryCompany, object>>[] { c => c.Drivers });
        }

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryDriver>> GetPagedDriversAsync(int pageIndex, int pageSize)
        {
            return await _unitOfWork.DeliveryDrivers.GetPagedAsync(
                pageIndex, 
                pageSize, 
                orderBy: q => q.OrderByDescending(d => d.Id),
                includes: new System.Linq.Expressions.Expression<Func<DeliveryDriver, object>>[] { d => d.User!, d => d.DeliveryCompany! });
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

        public async Task<Bolcko.Domain.Common.IPagedList<DeliveryJob>> GetPagedAllJobsAsync(int pageIndex, int pageSize, DeliveryJobStatus? statusFilter = null)
        {
            System.Linq.Expressions.Expression<Func<DeliveryJob, bool>>? predicate = null;
            if (statusFilter.HasValue)
            {
                predicate = j => j.Status == statusFilter.Value;
            }

            return await _unitOfWork.DeliveryJobs.GetPagedAsync(
                pageIndex, 
                pageSize, 
                predicate: predicate,
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

        #region LOG-06 Instant CliQ Carrier Wallet Payout & Escrow Release
        public async Task<Bolcko.Domain.Entities.Delivery.DTOs.CarrierPayoutResultDto> ReleaseJobsiteOtpPayoutAsync(int jobId, string otpCode, string? receiverNotes = null)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                .Include(j => j.Driver)
                    .ThenInclude(d => d!.User)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return new Bolcko.Domain.Entities.Delivery.DTOs.CarrierPayoutResultDto
                {
                    Success = false,
                    Message = "شحنة التوصيل غير موجودة."
                };
            }

            if (string.IsNullOrWhiteSpace(otpCode) || (job.DeliveryOtpCode != null && job.DeliveryOtpCode.Trim() != otpCode.Trim()))
            {
                return new Bolcko.Domain.Entities.Delivery.DTOs.CarrierPayoutResultDto
                {
                    Success = false,
                    JobId = jobId,
                    OrderId = job.OrderId,
                    Message = "رمز التحقق الميداني (e-POD OTP) غير صحيح! اطلب الرمز المكون من 6 أرقام من المقاول أو المشرف في الورشة."
                };
            }

            // 1. Update Job and Order Status to Delivered
            job.Status = DeliveryJobStatus.Delivered;
            job.DeliveredAt = DateTime.UtcNow;
            job.IsPodVerified = true;
            job.PodVerifiedAt = DateTime.UtcNow;

            if (job.Order != null)
            {
                job.Order.Status = OrderStatus.Delivered;
                _unitOfWork.Orders.Update(job.Order);
            }

            // 2. Determine Payout Amounts
            decimal grossFreight = job.DeliveryFee > 0 ? job.DeliveryFee : 35.00m;
            decimal platformTakeRate = job.PlatformFreightFee.HasValue && job.PlatformFreightFee.Value > 0
                ? job.PlatformFreightFee.Value
                : Math.Round(grossFreight * 0.07m, 2);
            decimal netPayout = Math.Round(grossFreight - platformTakeRate, 2);
            if (netPayout < 0) netPayout = 0;

            // 3. Resolve Beneficiary & CliQ Alias
            string cliqAlias = "CLIQ-CARRIER-PAYOUT";
            int? driverId = job.DriverId;
            int? companyId = job.DeliveryCompanyId;

            if (job.Driver != null)
            {
                job.Driver.TotalDeliveredOrders += 1;
                _unitOfWork.DeliveryDrivers.Update(job.Driver);
                cliqAlias = !string.IsNullOrWhiteSpace(job.Driver.CliqAlias) 
                    ? job.Driver.CliqAlias 
                    : (job.Driver.User?.PhoneNumber ?? "0790000000");
            }
            else if (job.Company != null)
            {
                cliqAlias = !string.IsNullOrWhiteSpace(job.Company.CliqAlias)
                    ? job.Company.CliqAlias
                    : (job.Company.PhoneNumber ?? "0780000000");
            }

            // 4. Generate e-POD Document Reference
            string epodDocUrl = $"/Delivery/Jobsite/POD/{job.OrderId}";

            // 5. Create Carrier Payout Transaction (Escrow Released)
            var txn = new Bolcko.Domain.Entities.Financing.CarrierPayoutTransaction
            {
                DeliveryJobId = job.Id,
                OrderId = job.OrderId,
                DriverId = driverId,
                DeliveryCompanyId = companyId,
                GrossFreightAmountJod = grossFreight,
                PlatformTakeRateFee = platformTakeRate,
                PayoutAmountJod = netPayout,
                PayoutMethod = "CliQ",
                DestinationCliqAlias = cliqAlias,
                TransactionReference = $"CLIQ-EPOD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}",
                Status = "Completed",
                InitiatedAt = DateTime.UtcNow,
                SettledAt = DateTime.UtcNow,
                DeliveryOtpVerified = otpCode.Trim(),
                EpodDocumentUrl = epodDocUrl,
                IsEscrowReleased = true
            };

            await _unitOfWork.CarrierPayoutTransactions.AddAsync(txn);
            _unitOfWork.DeliveryJobs.Update(job);
            await _unitOfWork.CompleteAsync();

            // 6. Send Notifications
            if (job.Driver?.UserId != null)
            {
                await _notificationService.SendNotificationToUserAsync(
                    job.Driver.UserId,
                    "🎉 إيداع فوري لمستحقات الشحن عبر CliQ",
                    $"تم توثيق تسليم الشحنة #{job.OrderId} بنجاح! تم تحرير الضمان المالي وإيداع صافي أجور النقل ({netPayout:N2} د.أ) فوراً إلى حساب CliQ ({cliqAlias}).",
                    epodDocUrl);
            }

            return new Bolcko.Domain.Entities.Delivery.DTOs.CarrierPayoutResultDto
            {
                Success = true,
                Message = $"تم إثبات التسليم الرقمي (e-POD) بنجاح! تم تحرير الضمان المالي وإيداع {netPayout:N2} د.أ فوراً إلى حساب CliQ ({cliqAlias})، وتوليد وثيقة التسليم.",
                JobId = job.Id,
                OrderId = job.OrderId,
                GrossFreightAmount = grossFreight,
                PlatformTakeRate = platformTakeRate,
                PayoutAmountJod = netPayout,
                TransactionReference = txn.TransactionReference,
                CliqAlias = cliqAlias,
                EpodDocumentUrl = epodDocUrl,
                IsEscrowReleased = true,
                SettledAt = txn.SettledAt ?? DateTime.UtcNow
            };
        }

        public async Task<bool> DisputeDeliveryJobAsync(int jobId, string disputeReason, string? photoEvidenceUrl = null)
        {
            var job = await _unitOfWork.DeliveryJobs.GetByIdAsync(jobId);
            if (job == null) return false;

            job.ReturnReason = disputeReason;
            _unitOfWork.DeliveryJobs.Update(job);

            var txn = new Bolcko.Domain.Entities.Financing.CarrierPayoutTransaction
            {
                DeliveryJobId = job.Id,
                OrderId = job.OrderId,
                DriverId = job.DriverId,
                DeliveryCompanyId = job.DeliveryCompanyId,
                GrossFreightAmountJod = job.DeliveryFee,
                PayoutAmountJod = 0.00m,
                PayoutMethod = "CliQ",
                TransactionReference = $"CLIQ-DISPUTE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                Status = "FrozenDisputed",
                InitiatedAt = DateTime.UtcNow,
                IsEscrowReleased = false,
                DisputeReason = disputeReason,
                DisputePhotoEvidenceUrl = photoEvidenceUrl,
                DisputedAt = DateTime.UtcNow
            };

            await _unitOfWork.CarrierPayoutTransactions.AddAsync(txn);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<Bolcko.Domain.Entities.Financing.CarrierPayoutTransaction?> GetPayoutTransactionByJobIdAsync(int jobId)
        {
            return await _unitOfWork.CarrierPayoutTransactions.GetAllAsQueryable()
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(t => t.DeliveryJobId == jobId);
        }
        #endregion
    }
}
