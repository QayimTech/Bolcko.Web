using System.Threading.Tasks;

namespace Bolcko.Domain.Interfaces
{
    public interface IBulkImportService
    {
        /// <summary>
        /// Processes a unified Excel file. Returns a detailed ImportResult with per-row outcomes.
        /// Columns may be Arabic or English. SKU taken from sheet if present, else auto-generated.
        /// </summary>
        Task<ImportResult> ProcessUnifiedExcelImportAsync(string filePath, string? localImageFolderPath = null);

        /// <summary>
        /// Imports data from a Google Sheet URL, optionally loading images from a local folder.
        /// </summary>
        Task<ImportResult> ProcessGoogleSheetImportAsync(string googleSheetUrl, string? localImageFolderPath = null);

        // Background Job Runners
        Task ProcessUnifiedExcelImportJobAsync(string importId, string filePath, string? zipFilePath = null);
        Task ProcessGoogleSheetImportJobAsync(string importId, string googleSheetUrl, string? zipFilePath = null);
    }
}
