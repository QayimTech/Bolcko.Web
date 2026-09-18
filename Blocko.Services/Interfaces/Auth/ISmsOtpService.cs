using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.Auth
{
    public interface ISmsOtpService
    {
        Task<string> GenerateOtpAsync(string phoneNumber, string purpose);
        Task<bool> VerifyOtpAsync(string phoneNumber, string code, string purpose);
    }
}
