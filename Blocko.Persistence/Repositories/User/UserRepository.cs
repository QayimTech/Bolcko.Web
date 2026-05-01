using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.User
{
    public class UserRepository : GenericRepository<Bolcko.Domain.Entities.User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }
        
        public async Task<Bolcko.Domain.Entities.User?> GetByEmailAsync(string email) => 
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}