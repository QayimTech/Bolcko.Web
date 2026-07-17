
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

    public async Task CleanOldLogsAsync(int daysToKeep = 10)
    {
        var logsPath = Path.Combine(_webHostEnvironment.ContentRootPath, "logs");
        if (!Directory.Exists(logsPath))
        {
            _logger.LogInformation("Logs directory doesn't exist, nothing to clean");
            return;
        }

        try
        {
            var logFiles = Directory.GetFiles(logsPath, "*.txt");
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var deletedCount = 0;

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

            _logger.LogInformation("Log cleanup complete! Deleted {DeletedCount} old logs", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log cleanup");
        }

        await Task.CompletedTask;
    }
}
