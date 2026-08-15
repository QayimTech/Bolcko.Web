
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LogController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LogController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index(string? fileName = null, int? pageNumber = 1, int? pageSize = 100)
        {
            var logFiles = new List<string>();
            var logEntries = new List<LogEntryViewModel>();
            
            try
            {
                // Fallback strategy for log directory resolution
                var logsDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "logs");
                if (!Directory.Exists(logsDirectory))
                {
                    logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                }

                if (Directory.Exists(logsDirectory))
                {
                    var txtFiles = Directory.GetFiles(logsDirectory, "*.txt");
                    var logExtFiles = Directory.GetFiles(logsDirectory, "*.log");

                    logFiles = txtFiles.Concat(logExtFiles)
                        .Select(Path.GetFileName)
                        .Where(f => !string.IsNullOrEmpty(f))
                        .OrderByDescending(x => x)
                        .ToList()!;
                }

                // Set default file name if none selected
                fileName ??= logFiles.FirstOrDefault();

                if (!string.IsNullOrEmpty(fileName))
                {
                    var filePath = Path.Combine(logsDirectory, fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        var lines = new List<string>();
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs))
                        {
                            string? line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                lines.Add(line);
                            }
                        }

                        foreach (var line in lines.AsEnumerable().Reverse())
                        {
                            logEntries.Add(new LogEntryViewModel
                            {
                                RawText = line
                            });
                        }

                        var size = pageSize.HasValue && pageSize.Value > 0 ? pageSize.Value : 100;
                        var page = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
                        logEntries = logEntries.Skip((page - 1) * size).Take(size).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logEntries.Add(new LogEntryViewModel 
                { 
                    RawText = $"[خطأ أثناء تحميل السجلات]: {ex.Message}" 
                });
            }

            var model = new LogsViewModel
            {
                AvailableLogFiles = logFiles,
                SelectedFileName = fileName,
                LogEntries = logEntries,
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 100
            };

            return View(model);
        }

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
}

