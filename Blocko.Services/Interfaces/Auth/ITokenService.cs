using System.Threading.Tasks;
using Bolcko.Domain.Entities.User;

namespace Blocko.Services.Interfaces.Auth
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(Bolcko.Domain.Entities.User.User user);
    }
}
