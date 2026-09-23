using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Delivery.DTOs;

namespace Blocko.Services.Interfaces.Delivery
{
    public interface IMaskedCommService
    {
        string MaskPhoneNumber(string? phone);
        Task<MaskedContactInfoDto> GetMaskedContactInfoAsync(int jobId, int? currentUserId = null);
        Task<List<DeliveryTripMessageDto>> GetTripMessagesAsync(int jobId);
        Task<DeliveryTripMessageDto> SendTripMessageAsync(SendTripMessageRequest request, int? senderUserId = null);
        Task<WhatsAppNotificationResultDto> SendOfficialWhatsAppMilestoneAsync(SendOfficialWhatsAppNotificationRequest request);
    }
}
