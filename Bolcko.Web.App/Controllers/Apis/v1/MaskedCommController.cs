using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Delivery.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    /// <summary>
    /// [Anti-Leak Comm] LOG-07: Masked In-App Load Communication & Official WhatsApp Notifications
    /// Protects personal contact numbers to prevent off-platform disintermediation.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MaskedCommController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public MaskedCommController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// Retrieves masked contact information and safe in-app calling token for a shipment.
        /// </summary>
        [HttpGet("job/{jobId}/contact")]
        public async Task<IActionResult> GetMaskedContactInfo(int jobId)
        {
            int? currentUserId = null;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var contactInfo = await _serviceManager.MaskedCommService.GetMaskedContactInfoAsync(jobId, currentUserId);
            return Ok(new
            {
                success = true,
                data = contactInfo,
                securityNotice = "أرقام الهواتف الشخصية مشفرة ومحمية بواسطة بوابة Block-O Anti-Disintermediation Shield."
            });
        }

        /// <summary>
        /// Retrieves in-trip chat history for gate coordinates, unloading directions, and crane access.
        /// </summary>
        [HttpGet("job/{jobId}/messages")]
        public async Task<IActionResult> GetTripMessages(int jobId)
        {
            var messages = await _serviceManager.MaskedCommService.GetTripMessagesAsync(jobId);
            return Ok(new
            {
                success = true,
                data = messages,
                totalCount = messages.Count
            });
        }

        /// <summary>
        /// Sends an in-trip masked chat message.
        /// </summary>
        [HttpPost("job/{jobId}/send")]
        public async Task<IActionResult> SendTripMessage(int jobId, [FromBody] SendTripMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MessageText))
            {
                return BadRequest(new { success = false, message = "يرجى كتابة نص الرسالة للتواصل مع الطرف الآخر." });
            }

            request.JobId = jobId;

            int? currentUserId = null;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var parsedId))
            {
                currentUserId = parsedId;
            }

            // Infer sender role if not explicitly provided
            if (string.IsNullOrWhiteSpace(request.SenderRole))
            {
                if (User.IsInRole("Driver") || User.IsInRole("DeliveryDriver"))
                    request.SenderRole = "Driver";
                else if (User.IsInRole("Vendor") || User.IsInRole("Supplier"))
                    request.SenderRole = "Vendor";
                else if (User.IsInRole("Contractor") || User.IsInRole("Client"))
                    request.SenderRole = "Contractor";
                else
                    request.SenderRole = "Driver";
            }

            var messageDto = await _serviceManager.MaskedCommService.SendTripMessageAsync(request, currentUserId);
            return Ok(new
            {
                success = true,
                message = "تم إرسال الرسالة عبر القناة المؤمنة بنجاح.",
                data = messageDto
            });
        }

        /// <summary>
        /// Triggers official Block-O WhatsApp milestone notification to recipient without revealing driver's personal WhatsApp.
        /// </summary>
        [HttpPost("job/{jobId}/whatsapp-dispatch")]
        public async Task<IActionResult> SendOfficialWhatsAppDispatch(int jobId, [FromBody] SendOfficialWhatsAppNotificationRequest request)
        {
            if (request == null)
            {
                request = new SendOfficialWhatsAppNotificationRequest();
            }

            request.JobId = jobId;
            var result = await _serviceManager.MaskedCommService.SendOfficialWhatsAppMilestoneAsync(request);
            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                data = result
            });
        }
    }
}
