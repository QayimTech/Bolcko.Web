using Blocko.Services.Interfaces.User;
using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.User
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Bolcko.Domain.Entities.User?> AuthenticateAsync(string email, string password)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null || user.PasswordHash != password) return null;
            return user;
        }

        public async Task<Bolcko.Domain.Entities.User> RegisterUserAsync(Bolcko.Domain.Entities.User user, string password)
        {
            user.PasswordHash = password;
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();
            return user;
        }

        public async Task<Bolcko.Domain.Entities.User?> GetUserByIdAsync(int id) => await _unitOfWork.Users.GetByIdAsync(id);
    }
}