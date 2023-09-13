namespace XlentLink.Commons.Logging;

public interface ISyncLoggerFactory
{
    ISyncLogger CreateForCategory(string category);
}

public interface ISyncLogger
{
    void LogSync(LogRecord logRecord);

    void LogCritical(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
    void LogError(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
    void LogWarning(string message, Exception? exception = null, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
    void LogInformation(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
    void LogDebug(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
    void LogTrace(string message, string? location = null, IReadOnlyDictionary<string, object?>? data = null);
}
