using Microsoft.Extensions.Logging;

namespace ChatApplication.Infrastructure.Logging;

public class LoggerService
{
    private readonly ILogger<LoggerService> _logger;

    public LoggerService(ILogger<LoggerService> logger) => _logger = logger;

    public void LogInfo(string message) => _logger.LogInformation(message);
    public void LogWarning(string message) => _logger.LogWarning(message);
    public void LogError(string message, Exception? ex = null) => _logger.LogError(ex, message);
}
