using Microsoft.Extensions.Logging;

namespace XlentLink.AspNet.Common.Logging;

public class LogRecord
{
    public LogLevel LogLevel { get; set; }
    
    public string Message { get; set; } = null!;

    public string? Location { get; set; }

    public IReadOnlyDictionary<string, object?>? Data { get; set; }

    public Exception? Exception { get; set; }
}