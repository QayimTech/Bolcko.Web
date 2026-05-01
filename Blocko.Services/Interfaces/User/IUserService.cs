using Bolcko.Domain.Entities;

namespace Blocko.Services.Interfaces.User
{
    public interface IUserService
    {
        Task<Bolcko.Domain.Entities.User?> AuthenticateAsync(string email, string password);
        Task<Bolcko.Domain.Entities.User> RegisterUserAsync(Bolcko.Domain.Entities.User user, string password);
        Task<Bolcko.Domain.Entities.User?> GetUserByIdAsync(int id);
    }
}