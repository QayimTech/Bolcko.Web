using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.SEO.DTOs;

namespace Blocko.Services.Interfaces.SEO
{
    public interface ISEOService
    {
        Task<IPagedList<SEOMetadataDto>> GetPagedSEOAsync(int pageIndex, int pageSize);
        Task<IEnumerable<SEOMetadataDto>> GetAllSEOAsync();
        Task<SEOMetadataDto?> GetSEOByPageNameAsync(string pageName);
        Task<SEOMetadataDto?> GetSEOByIdAsync(int id);
        Task AddOrUpdateSEOAsync(SEOMetadataDto seoDto);
        Task DeleteSEOAsync(int id);
    }
}
