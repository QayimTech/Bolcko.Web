using Bolcko.Domain.Entities.User;

namespace Bolcko.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<Bolcko.Domain.Entities.User.User> 
    {
        Task<Bolcko.Domain.Entities.User.User?> GetByEmailAsync(string email);
    }
}