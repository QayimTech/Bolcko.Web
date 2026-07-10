using System.Threading.Tasks;

namespace Bolcko.Domain.Interfaces
{
    public interface IBulkImportService
    {
        /// <summary>
        /// Processes a unified Excel file. Returns a detailed ImportResult with per-row outcomes.
        /// Columns may be Arabic or English. SKU taken from sheet if present, else auto-generated.
        /// </summary>
        Task<ImportResult> ProcessUnifiedExcelImportAsync(string filePath);

        /// <summary>
        /// Processes a unified JSON file containing categories and products arrays.
        /// SKU is auto-generated. Supports imageBase64 field for images.
        /// </summary>
        Task<ImportResult> ProcessUnifiedJsonImportAsync(string filePath);

        // Legacy — kept for backward compatibility with already-enqueued Hangfire jobs
        Task ProcessProductImportAsync(string filePath);
        Task ProcessCategoryImportAsync(string filePath);
    }
}
