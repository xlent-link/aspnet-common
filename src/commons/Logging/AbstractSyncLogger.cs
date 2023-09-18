using Microsoft.Extensions.Logging;

namespace XlentLink.AspNet.Common.Logging;

public abstract class AbstractSyncLogger : ISyncLogger
{
    public abstract void LogSync(LogRecord logRecord);

    public void LogCritical(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Critical,
            Message = message,
            Exception = exception,
            Location = location,
            Data = data
        });
    }

    public void LogError(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Error,
            Message = message,
            Exception = exception,
            Location = location,
            Data = data
        });
    }

    public void LogWarning(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Warning,
            Message = message,
            Exception = exception,
            Location = location,
            Data = data
        });
    }

    public void LogInformation(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Information,
            Message = message,
            Location = location,
            Data = data
        });
    }

    public void LogDebug(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Debug,
            Message = message,
            Location = location,
            Data = data
        });
    }

    public void LogTrace(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null)
    {
        LogSync(new()
        {
            LogLevel = LogLevel.Trace,
            Message = message,
            Location = location,
            Data = data
        });
    }
}