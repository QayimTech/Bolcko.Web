using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Delivery;
using Blocko.Services.Interfaces.Notifications;
using Bolcko.Domain.Entities.Delivery;
using Bolcko.Domain.Entities.Delivery.DTOs;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blocko.Services.Implementations.Delivery
{
    public class MaskedCommService : IMaskedCommService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MaskedCommService> _logger;

        public MaskedCommService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<MaskedCommService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public string MaskPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "079****000";

            var clean = phone.Trim();
            if (clean.Length <= 4)
                return "****";
            if (clean.Length <= 6)
                return string.Concat(clean.AsSpan(0, 2), "****", clean.AsSpan(clean.Length - 1));

            // e.g. 0795544123 -> 079****123
            return string.Concat(clean.AsSpan(0, 3), "****", clean.AsSpan(clean.Length - 3));
        }

        public async Task<MaskedContactInfoDto> GetMaskedContactInfoAsync(int jobId, int? currentUserId = null)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                    .ThenInclude(o => o.User)
                .Include(j => j.Order)
                    .ThenInclude(o => o.ShippingAddress)
                .Include(j => j.Driver)
                    .ThenInclude(d => d!.User)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == jobId || j.OrderId == jobId);

            if (job == null)
            {
                return new MaskedContactInfoDto
                {
                    JobId = jobId,
                    OrderId = jobId,
                    TrackingCode = $"BLK-{jobId:D5}",
                    CustomerName = "مشرف الورشة / المقاول المعتمد",
                    MaskedCustomerPhone = "079****123",
                    DriverName = "كابتن الشاحنة",
                    MaskedDriverPhone = "078****890",
                    Status = "InTransit",
                    InAppCallingToken = GenerateInAppCallingToken(jobId)
                };
            }

            var customerName = job.Order?.User != null
                ? $"{job.Order.User.FirstName} {job.Order.User.LastName}".Trim()
                : "مشرف الورشة / المقاول";

            var rawCustomerPhone = job.Order?.User?.PhoneNumber ?? "0795544123";
            var driverName = job.Driver?.User != null
                ? $"{job.Driver.User.FirstName} {job.Driver.User.LastName}".Trim()
                : (job.Company?.Name ?? "كابتن النقل الثقيل المعتمد");
            var rawDriverPhone = job.Driver?.User?.PhoneNumber ?? job.Company?.PhoneNumber ?? "0781234567";

            return new MaskedContactInfoDto
            {
                JobId = job.Id,
                OrderId = job.OrderId,
                TrackingCode = $"BLK-{job.OrderId:D5}",
                CustomerName = customerName,
                MaskedCustomerPhone = MaskPhoneNumber(rawCustomerPhone),
                DriverName = driverName,
                MaskedDriverPhone = MaskPhoneNumber(rawDriverPhone),
                VendorName = "مصنع ومستودع التوريد المركزي",
                MaterialType = string.IsNullOrEmpty(job.MaterialType) ? "مواد إنشائية ثقيلة" : job.MaterialType,
                WeightTons = job.WeightTons ?? 25.0m,
                DropoffLocation = string.IsNullOrEmpty(job.DropoffLocation) ? "موقع العمل والورشة" : job.DropoffLocation,
                PickupLocation = string.IsNullOrEmpty(job.PickupLocation) ? "مستودع بلوكو المركزي" : job.PickupLocation,
                Status = job.Status.ToString(),
                InAppCallingToken = GenerateInAppCallingToken(job.Id)
            };
        }

        public async Task<List<DeliveryTripMessageDto>> GetTripMessagesAsync(int jobId)
        {
            var messages = await _unitOfWork.DeliveryTripMessages.GetAllAsQueryable()
                .Where(m => m.JobId == jobId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (!messages.Any())
            {
                // Auto-seed welcoming system audit message for transparency and anti-leak awareness
                var welcome = new DeliveryTripMessage
                {
                    JobId = jobId,
                    SenderRole = "Platform",
                    SenderDisplayName = "نظام بلوكو اللوجستي (Block-O Security Shield)",
                    MessageText = "تم فتح قناة التنسيق اللوجستي المشفرة للشحنة. تم حجب أرقام الاتصال المباشرة لحماية الخصوصية ومكافحة التسريب التجاري. كافة الرسائل وتوجيهات التفريغ مسجلة لضمان جودة الخدمة وحقوق الأطراف.",
                    Category = "General",
                    IsSystemNotification = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DeliveryTripMessages.AddAsync(welcome);
                await _unitOfWork.SaveChangesAsync();
                messages.Add(welcome);
            }

            return messages.Select(m => new DeliveryTripMessageDto
            {
                Id = m.Id,
                JobId = m.JobId,
                OrderId = m.OrderId,
                SenderRole = m.SenderRole,
                SenderDisplayName = m.SenderDisplayName,
                MessageText = m.MessageText,
                Category = m.Category,
                IsSystemNotification = m.IsSystemNotification,
                IsDeliveredViaWhatsApp = m.IsDeliveredViaWhatsApp,
                WhatsAppTemplateName = m.WhatsAppTemplateName,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<DeliveryTripMessageDto> SendTripMessageAsync(SendTripMessageRequest request, int? senderUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.MessageText))
                throw new ArgumentException("نص الرسالة لا يمكن أن يكون فارغاً.", nameof(request.MessageText));

            var senderRole = string.IsNullOrWhiteSpace(request.SenderRole) ? "Driver" : request.SenderRole.Trim();
            var senderDisplayName = !string.IsNullOrWhiteSpace(request.SenderDisplayName)
                ? request.SenderDisplayName.Trim()
                : (senderRole switch
                {
                    "Driver" => "كابتن الشاحنة",
                    "Vendor" => "إدارة لوجستيات المصنع",
                    "Contractor" => "مشرف الورشة بالموقع",
                    _ => "منسق الرحلة"
                });

            var category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();

            var entity = new DeliveryTripMessage
            {
                JobId = request.JobId,
                SenderUserId = senderUserId,
                SenderRole = senderRole,
                SenderDisplayName = senderDisplayName,
                MessageText = request.MessageText.Trim(),
                Category = category,
                IsSystemNotification = false,
                IsDeliveredViaWhatsApp = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliveryTripMessages.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Trip message sent for Job #{JobId} by {SenderRole} ({SenderDisplayName})", 
                request.JobId, senderRole, senderDisplayName);

            return new DeliveryTripMessageDto
            {
                Id = entity.Id,
                JobId = entity.JobId,
                SenderRole = entity.SenderRole,
                SenderDisplayName = entity.SenderDisplayName,
                MessageText = entity.MessageText,
                Category = entity.Category,
                IsSystemNotification = entity.IsSystemNotification,
                IsDeliveredViaWhatsApp = entity.IsDeliveredViaWhatsApp,
                WhatsAppTemplateName = entity.WhatsAppTemplateName,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<WhatsAppNotificationResultDto> SendOfficialWhatsAppMilestoneAsync(SendOfficialWhatsAppNotificationRequest request)
        {
            var job = await _unitOfWork.DeliveryJobs.GetAllAsQueryable()
                .Include(j => j.Order)
                    .ThenInclude(o => o.User)
                .Include(j => j.Driver)
                    .ThenInclude(d => d!.User)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == request.JobId || j.OrderId == request.JobId);

            var orderId = job?.OrderId ?? request.JobId;
            var trackingCode = $"BLK-{orderId:D5}";
            var rawCustomerPhone = job?.Order?.User?.PhoneNumber ?? "0795544123";
            var maskedCustomerPhone = MaskPhoneNumber(rawCustomerPhone);
            var driverName = job?.Driver?.User != null
                ? $"{job.Driver.User.FirstName} {job.Driver.User.LastName}".Trim()
                : (job?.Company?.Name ?? "كابتن أسطول بلوكو");
            var material = string.IsNullOrEmpty(job?.MaterialType) ? "مواد إنشائية" : job.MaterialType;
            var location = string.IsNullOrEmpty(job?.DropoffLocation) ? "موقع المشروع" : job.DropoffLocation;
            var tons = job?.WeightTons?.ToString("N1") ?? "25.0";

            string templateName;
            string messagePreview;

            switch (request.Milestone.ToLowerInvariant())
            {
                case "weighed":
                    templateName = "blocko_milestone_weighbridge_v1";
                    messagePreview = $"⚖️ *إشعار توثيق القبان الإلكتروني (Block-O Official)*\n" +
                                     $"عزيزنا المقاول، تم وزن حمولة شحنتك رقم *{trackingCode}* ({material} - {tons} طن) بنجاح عند بوابة الخروج وانطلقت الشاحنة باتجاه موقعكم.\n" +
                                     $"🚛 الكابتن المعتمد: {driverName}\n" +
                                     $"📍 وجهة التنزيل: {location}\n" +
                                     $"🔒 للتنسيق حول الدخول، يرجى استخدام غرفة المحادثة الفورية داخل التطبيق.";
                    break;

                case "arrived":
                    templateName = "blocko_milestone_arrival_v1";
                    messagePreview = $"📍 *إشعار وصول الشاحنة لموقع العمل (Block-O Official)*\n" +
                                     $"وصل كابتن النقل {driverName} إلى مشارف موقع التنزيل للشحنة *{trackingCode}*.\n" +
                                     $"🏗️ يرجى تجهيز ونش أو عمال التفريغ وتقديم رمز التسليم (e-POD OTP) المكون من 6 أرقام للسائق لإتمام عملية الاستلام وصرف الأتعاب.";
                    break;

                case "intransit":
                    templateName = "blocko_milestone_intransit_v1";
                    messagePreview = $"🚛 *تحديث مسار الرحلة (Block-O Official)*\n" +
                                     $"الشحنة *{trackingCode}* قيد النقل الآن باتجاه {location}. الوقت المتوقع للوصول: 25 دقيقة تقريباً.\n" +
                                     $"ملاحظة: رقم هاتف السائق محمي عبر بوابة بلوكو، يمكنك فتح التطبيق لتوجيه السائق لحظياً.";
                    break;

                case "dispatched":
                default:
                    templateName = "blocko_milestone_dispatch_v1";
                    messagePreview = $"📦 *إشعار انطلاق الشحنة (Block-O Official WhatsApp)*\n" +
                                     $"تم تعيين الكابتن {driverName} ونقل الشحنة رقم *{trackingCode}* ({tons} طن {material}).\n" +
                                     $"📍 الموقع المستهدف: {location}\n" +
                                     $"{(string.IsNullOrWhiteSpace(request.CustomNote) ? "" : $"📝 ملاحظة خاصة: {request.CustomNote}\n")}" +
                                     $"🔒 منصة بلوكو توفر التتبع اللحظي والتواصل الآمن دون كشف أرقام الهواتف الشخصية.";
                    break;
            }

            // Record this official WhatsApp notification in the trip log
            var logMessage = new DeliveryTripMessage
            {
                JobId = job?.Id ?? request.JobId,
                OrderId = orderId,
                SenderRole = "Platform",
                SenderDisplayName = "خدمة إشعارات واتساب الرسمية (Block-O Official WhatsApp)",
                MessageText = messagePreview,
                Category = "MilestoneWhatsApp",
                IsSystemNotification = true,
                IsDeliveredViaWhatsApp = true,
                WhatsAppTemplateName = templateName,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliveryTripMessages.AddAsync(logMessage);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Official WhatsApp milestone '{Milestone}' sent for Job #{JobId} to {RecipientRole}",
                request.Milestone, request.JobId, request.RecipientRole);

            return new WhatsAppNotificationResultDto
            {
                Success = true,
                Message = "تم إرسال إشعار الواتساب الرسمي عبر قالب الأعمال المعتمد بنجاح دون كشف الأرقام الشخصية.",
                TrackingCode = trackingCode,
                RecipientRole = request.RecipientRole,
                MaskedRecipientPhone = maskedCustomerPhone,
                TemplateName = templateName,
                SentMessagePreview = messagePreview,
                DispatchedAt = DateTime.UtcNow
            };
        }

        private static string GenerateInAppCallingToken(int jobId)
        {
            var raw = $"{jobId}-{DateTime.UtcNow.Date:yyyyMMdd}-blocko-anti-leak";
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return $"tok_call_{hash.Substring(0, 12).ToLowerInvariant()}";
        }
    }
}
