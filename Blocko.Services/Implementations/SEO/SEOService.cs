using Blocko.Persistence.Common;
using Blocko.Services.Interfaces.SEO;
using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.SEO;
using Bolcko.Domain.Entities.SEO.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.SEO
{
    public class SEOService : ISEOService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SEOService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IPagedList<SEOMetadataDto>> GetPagedSEOAsync(int pageIndex, int pageSize)
        {
            var paged = await _unitOfWork.SEO.GetPagedAsync(
                pageIndex,
                pageSize,
                orderBy: q => q.OrderBy(s => s.PageOrder).ThenBy(s => s.PageName));

            var dtos = paged.Items.Select(s => new SEOMetadataDto
            {
                Id = s.Id,
                PageName = s.PageName,
                PageTitle = s.PageTitle,
                MetaDescription = s.MetaDescription,
                MetaKeywords = s.MetaKeywords,
                PageUrl = s.PageUrl,
                PageOrder = s.PageOrder
            });

            return new PagedList<SEOMetadataDto>(dtos, paged.TotalCount, paged.PageIndex, paged.PageSize);
        }

        public async Task<IEnumerable<SEOMetadataDto>> GetAllSEOAsync()
        {
            var seoList = await _unitOfWork.SEO.GetAllAsync();
            return seoList.Select(s => new SEOMetadataDto
            {
                Id = s.Id,
                PageName = s.PageName,
                PageTitle = s.PageTitle,
                MetaDescription = s.MetaDescription,
                MetaKeywords = s.MetaKeywords,
                PageUrl = s.PageUrl,
                PageOrder = s.PageOrder
            });
        }

        public async Task<SEOMetadataDto?> GetSEOByPageNameAsync(string pageName)
        {
            var s = await _unitOfWork.SEO.GetByPageNameAsync(pageName);
            if (s == null) return null;
            return new SEOMetadataDto
            {
                Id = s.Id,
                PageName = s.PageName,
                PageTitle = s.PageTitle,
                MetaDescription = s.MetaDescription,
                MetaKeywords = s.MetaKeywords,
                PageUrl = s.PageUrl,
                PageOrder = s.PageOrder
            };
        }

        public async Task<SEOMetadataDto?> GetSEOByIdAsync(int id)
        {
            var s = await _unitOfWork.SEO.GetByIdAsync(id);
            if (s == null) return null;
            return new SEOMetadataDto
            {
                Id = s.Id,
                PageName = s.PageName,
                PageTitle = s.PageTitle,
                MetaDescription = s.MetaDescription,
                MetaKeywords = s.MetaKeywords,
                PageUrl = s.PageUrl,
                PageOrder = s.PageOrder
            };
        }

        public async Task AddOrUpdateSEOAsync(SEOMetadataDto seoDto)
        {
            if (seoDto.Id == 0)
            {
                var seo = new SEOMetadata
                {
                    PageName = seoDto.PageName,
                    PageTitle = seoDto.PageTitle,
                    MetaDescription = seoDto.MetaDescription,
                    MetaKeywords = seoDto.MetaKeywords,
                    PageUrl = seoDto.PageUrl,
                    PageOrder = seoDto.PageOrder,
                    LastUpdated = DateTime.UtcNow
                };
                await _unitOfWork.SEO.AddAsync(seo);
            }
            else
            {
                var seo = await _unitOfWork.SEO.GetByIdAsync(seoDto.Id);
                if (seo != null)
                {
                    seo.PageName = seoDto.PageName;
                    seo.PageTitle = seoDto.PageTitle;
                    seo.MetaDescription = seoDto.MetaDescription;
                    seo.MetaKeywords = seoDto.MetaKeywords;
                    seo.PageUrl = seoDto.PageUrl;
                    seo.PageOrder = seoDto.PageOrder;
                    seo.LastUpdated = DateTime.UtcNow;
                    _unitOfWork.SEO.Update(seo);
                }
            }
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteSEOAsync(int id)
        {
            var seo = await _unitOfWork.SEO.GetByIdAsync(id);
            if (seo != null)
            {
                _unitOfWork.SEO.Remove(seo);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}
