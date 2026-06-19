using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Project;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Category
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProjectService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<Project>> GetUserProjectsAsync(int userId)
        {
            return await _unitOfWork.Projects.GetUserProjectsAsync(userId);
        }
    }
}
