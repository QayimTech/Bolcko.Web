using Bolcko.Domain.Entities;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Blocko.Persistence.Repositories.Project
{
    public class ProjectRepository : GenericRepository<Bolcko.Domain.Entities.Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Bolcko.Domain.Entities.Project>> GetUserProjectsAsync(int userId) => 
            await _context.Projects.Where(p => p.UserId == userId).ToListAsync();
    }
}