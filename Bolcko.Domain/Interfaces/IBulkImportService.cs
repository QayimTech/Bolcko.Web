using System.Threading.Tasks;

namespace Bolcko.Domain.Interfaces
{
    public interface IBulkImportService
    {
        /// <summary>
        /// Processes a unified Excel file (two sheets: Categories + Products).
        /// SKU is auto-generated. Columns may be Arabic or English.
        /// </summary>
        Task ProcessUnifiedExcelImportAsync(string filePath);

        /// <summary>
        /// Processes a unified JSON file containing categories and products arrays.
        /// SKU is auto-generated. Supports imageBase64 field for images.
        /// </summary>
        Task ProcessUnifiedJsonImportAsync(string filePath);

        // Legacy — kept for backward compatibility
        Task ProcessProductImportAsync(string filePath);
        Task ProcessCategoryImportAsync(string filePath);
    }
}
