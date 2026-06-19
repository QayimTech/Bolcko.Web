using Bolcko.Domain.Entities.Project;

namespace Blocko.Services.Interfaces.Category
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetUserProjectsAsync(int userId);
    }
}
