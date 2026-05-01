using Bolcko.Domain.Entities.Project;

namespace Bolcko.Domain.Interfaces
{
    public interface IProjectRepository : IGenericRepository<Bolcko.Domain.Entities.Project.Project> 
    {
        Task<IEnumerable<Bolcko.Domain.Entities.Project.Project>> GetUserProjectsAsync(int userId);
    }
}