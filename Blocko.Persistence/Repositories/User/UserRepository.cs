using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.User
{
    public class UserRepository : GenericRepository<Bolcko.Domain.Entities.User.User>, IUserRepository
    {
        public UserRepository(BlockoDbContext context) : base(context) { }
        
        public async Task<Bolcko.Domain.Entities.User.User?> GetByEmailAsync(string email) => 
            await _context.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
    }
}