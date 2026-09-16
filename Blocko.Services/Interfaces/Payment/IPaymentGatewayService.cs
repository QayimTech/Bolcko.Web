using System.Collections.Generic;
using System.Threading.Tasks;
using Bolcko.Domain.Entities.Payment;

namespace Blocko.Services.Interfaces.Payment
{
    public interface IPaymentGatewayService
    {
        Task<PaymentGatewaySettingsDto> GetSettingsAsync();
        Task SaveSettingsAsync(PaymentGatewaySettingsDto settings);
        Task<List<PaymentMethodOptionDto>> GetActivePaymentMethodsAsync(bool isArabic = true);
        Task<bool> IsPaymentMethodActiveAsync(string methodCode);
    }
}
