using Bolcko.Domain.Entities.SEO;

namespace Bolcko.Domain.Interfaces
{
    public interface ISEORepository : IGenericRepository<SEOMetadata>
    {
        Task<SEOMetadata?> GetByPageNameAsync(string pageName);
    }
}
