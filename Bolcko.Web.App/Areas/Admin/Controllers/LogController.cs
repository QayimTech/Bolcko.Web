
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
            
            // Fallback strategy for log directory resolution
            var logsDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "logs");
            if (!Directory.Exists(logsDirectory))
            {
                logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            }

            try
            {
                if (Directory.Exists(logsDirectory))
                {
                    logFiles = Directory.GetFiles(logsDirectory, "*.txt")
                        .Select(Path.GetFileName)
                        .OrderByDescending(x => x)
                        .ToList()!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogController] Error listing logs directory: {ex.Message}");
            }

            // Set default file name if none selected
            fileName ??= logFiles.FirstOrDefault();

            if (fileName != null)
            {
                var filePath = Path.Combine(logsDirectory, fileName);
                try
                {
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

                        // Parse lines (assuming Serilog default output format)
                        foreach (var line in lines.AsEnumerable().Reverse()) // Newest first
                        {
                            logEntries.Add(new LogEntryViewModel
                            {
                                RawText = line
                            });
                        }

                        // Apply pagination
                        pageSize ??= 100;
                        pageNumber ??= 1;
                        logEntries = logEntries.Skip(((int)pageNumber - 1) * (int)pageSize).Take((int)pageSize).ToList();
                    }
                }
                catch (Exception ex)
                {
                    logEntries.Add(new LogEntryViewModel 
                    { 
                        RawText = $"[خطأ أثناء قراءة ملف السجل]: {ex.Message}" 
                    });
                }
            }

            var model = new LogsViewModel
            {
                AvailableLogFiles = logFiles,
                SelectedFileName = fileName,
                LogEntries = logEntries,
                PageNumber = (int)pageNumber,
                PageSize = (int)pageSize
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

