using Blocko.Services.Interfaces.User;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.user
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null || user.PasswordHash != password) return null;
            return user;
        }

        public async Task<User> RegisterUserAsync(User user, string password)
        {
            user.PasswordHash = password;
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id) => await _unitOfWork.Users.GetByIdAsync(id);
    }
}