using Bolcko.Domain.Entities.Product.DTOs;
using System.IO;
using System.Threading.Tasks;

namespace Blocko.Services.Interfaces.SEO
{
    public interface IProductSeoService
    {
        Task<byte[]> ExportProductsSeoToExcelAsync(ProductSeoFilterParamsDto filter);
        Task<byte[]> ExportSingleProductSeoToExcelAsync(int productId);
        Task<BulkSeoJobResultDto> ImportProductSeoFromStreamAsync(Stream fileStream, bool autoApproveSeo = true);
        Task ProcessSeoBulkImportHangfireJobAsync(string jobId, string tempFilePath, bool autoApproveSeo);
        Task<BulkSeoJobResultDto?> GetSeoJobResultAsync(string jobId);
        Task<bool> UpdateSingleProductSeoAsync(ProductSeoImportDto dto);
        Task<(int total, int approved, int pending, int missingDesc)> GetSeoMetricsAsync();
    }
}
