
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
            var logsDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "logs");
            var logFiles = new List<string>();

            if (Directory.Exists(logsDirectory))
            {
                logFiles = Directory.GetFiles(logsDirectory, "*.txt")
                    .Select(Path.GetFileName)
                    .OrderByDescending(x => x)
                    .ToList()!;
            }

            // Set default file name if none selected
            fileName ??= logFiles.FirstOrDefault();

            var logEntries = new List<LogEntryViewModel>();

            if (fileName != null && System.IO.File.Exists(Path.Combine(logsDirectory, fileName)))
            {
                var lines = new List<string>();
                var filePath = Path.Combine(logsDirectory, fileName);

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

