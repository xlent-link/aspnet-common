using System.Text.Json;
using Microsoft.Extensions.Logging;
using XlentLink.Commons.Context;

namespace XlentLink.Commons.Logging;

public class ConsoleLoggerFactory : ISyncLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IContextValueProvider _contextValueProvider;

    public ConsoleLoggerFactory(ILoggerFactory loggerFactory, IContextValueProvider contextValueProvider)
    {
        _loggerFactory = loggerFactory;
        _contextValueProvider = contextValueProvider;
    }

    public ISyncLogger CreateForCategory(string category)
    {
        var logger = _loggerFactory.CreateLogger(category);
        return new ConsoleLogger(logger, _contextValueProvider);
    }
}

public class ConsoleLogger : AbstractSyncLogger
{
    private readonly ILogger _logger;
    private readonly IContextValueProvider _contextValueProvider;

    public ConsoleLogger(ILogger logger, IContextValueProvider contextValueProvider)
    {
        _logger = logger;
        _contextValueProvider = contextValueProvider;
    }

    public override void LogSync(LogRecord logRecord)
    {
        var message = "";
        if (_contextValueProvider.CorrelationId != null) message += $"|{_contextValueProvider.CorrelationId}";
        if (logRecord.Location != null) message += $"|{logRecord.Location}";
        if (logRecord.Data != null)
        {
            foreach (var entry in logRecord.Data)
            {
                message += $"\n      |data.{entry.Key} = {JsonSerializer.Serialize(entry.Value)}";
            }
        }
        message += $"\n    {logRecord.Message}";

        if (logRecord.Exception != null)
        {
            _logger.Log(logRecord.LogLevel, logRecord.Exception, message);
        }
        else
        {
            _logger.Log(logRecord.LogLevel, message);
        }
    }
}