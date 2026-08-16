
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;
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
                var searchDirs = new List<string>
                {
                    Path.Combine(_webHostEnvironment.ContentRootPath, "logs"),
                    Path.Combine(Directory.GetCurrentDirectory(), "logs"),
                    Path.Combine(_webHostEnvironment.WebRootPath ?? string.Empty, "logs")
                }.Where(d => !string.IsNullOrEmpty(d) && Directory.Exists(d)).Distinct().ToList();

                var foundFiles = new List<string>();
                foreach (var dir in searchDirs)
                {
                    try
                    {
                        var txtFiles = Directory.GetFiles(dir, "*.txt");
                        var logExtFiles = Directory.GetFiles(dir, "*.log");
                        foundFiles.AddRange(txtFiles.Concat(logExtFiles).Select(Path.GetFileName).Where(f => !string.IsNullOrEmpty(f))!);
                    }
                    catch { }
                }

                logFiles = foundFiles.Distinct().OrderByDescending(x => x).ToList()!;

                // Set default file name if none selected
                fileName ??= logFiles.FirstOrDefault();

                if (!string.IsNullOrEmpty(fileName))
                {
                    string? targetFilePath = null;
                    foreach (var dir in searchDirs)
                    {
                        var candidate = Path.Combine(dir, fileName);
                        if (System.IO.File.Exists(candidate))
                        {
                            targetFilePath = candidate;
                            break;
                        }
                    }

                    if (targetFilePath != null && System.IO.File.Exists(targetFilePath))
                    {
                        var lines = new List<string>();
                        using (var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
    }
}


