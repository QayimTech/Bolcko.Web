using System.Collections.Generic;

namespace Bolcko.Web.App.Areas.Admin.Models.ViewModels
{
    public class LogEntryViewModel
    {
        public string RawText { get; set; } = string.Empty;
    }

    public class LogsViewModel
    {
        public List<string> AvailableLogFiles { get; set; } = new();
        public string? SelectedFileName { get; set; }
        public List<LogEntryViewModel> LogEntries { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
