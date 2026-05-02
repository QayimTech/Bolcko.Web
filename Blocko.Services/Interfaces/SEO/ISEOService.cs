using Bolcko.Domain.Entities.SEO.DTOs;

namespace Blocko.Services.Interfaces.SEO
{
    public interface ISEOService
    {
        Task<IEnumerable<SEOMetadataDto>> GetAllSEOAsync();
        Task<SEOMetadataDto?> GetSEOByPageNameAsync(string pageName);
        Task<SEOMetadataDto?> GetSEOByIdAsync(int id);
        Task AddOrUpdateSEOAsync(SEOMetadataDto seoDto);
        Task DeleteSEOAsync(int id);
    }
}
