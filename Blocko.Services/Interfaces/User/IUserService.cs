namespace Blocko.Services.Interfaces.User
{
    public interface IUserService
    {
        Task<Bolcko.Domain.Entities.User.User?> AuthenticateAsync(string email, string password);
        Task<Bolcko.Domain.Entities.User.User> RegisterUserAsync(Bolcko.Domain.Entities.User.User user, string password);
        Task<Bolcko.Domain.Entities.User.User?> GetUserByIdAsync(int id);
    }
}