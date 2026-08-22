
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bolcko.Web.App.Services;

public class LogCleanupService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(IWebHostEnvironment webHostEnvironment, ILogger<LogCleanupService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task CleanOldLogsAsync(int daysToKeep = 12)
    {
        var searchPaths = new[]
        {
            Path.Combine(_webHostEnvironment.ContentRootPath, "logs"),
            Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, "logs"),
            Path.Combine(Directory.GetCurrentDirectory(), "logs")
        }.Distinct();

        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var deletedCount = 0;

        foreach (var logsPath in searchPaths)
        {
            if (!Directory.Exists(logsPath))
                continue;

            try
            {
                var logFiles = Directory.GetFiles(logsPath, "*.*"); // Clean all types of logs (txt, log)

                foreach (var logFile in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.LastWriteTimeUtc < cutoffDate)
                        {
                            fileInfo.Delete();
                            deletedCount++;
                            _logger.LogInformation("Deleted log file {LogFile}", fileInfo.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete log file {LogFile}", logFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup in path {LogsPath}", logsPath);
            }
        }

        _logger.LogInformation("Log cleanup complete! Deleted {DeletedCount} old logs", deletedCount);
        await Task.CompletedTask;
    }
}
