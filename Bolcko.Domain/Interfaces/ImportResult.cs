using System.Collections.Generic;

namespace Bolcko.Domain.Interfaces
{
    public class ImportResult
    {
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Updated { get; set; }
        public List<ImportRowResult> Rows { get; set; } = new();
        public string Summary => HasError ? ErrorMessage : $"تم رفع {Imported} منتج جديد, تحديث {Updated}, تخطي {Skipped} من أصل {TotalRows} سجل";
    }

    public class ImportRowResult
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public ImportRowStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public enum ImportRowStatus { Imported, Updated, Skipped }
}
