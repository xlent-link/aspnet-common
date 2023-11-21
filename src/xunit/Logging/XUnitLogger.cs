using System.Text.Json;
using XlentLink.AspNet.Common.Context;
using XlentLink.AspNet.Common.Logging;
using Xunit.Abstractions;

namespace XlentLink.AspNet.XUnit.Logging;

public class XUnitLogger : AbstractSyncLogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IContextValueProvider _contextValueProvider;

    public XUnitLogger(ITestOutputHelper testOutputHelper, IContextValueProvider contextValueProvider)
    {
        _testOutputHelper = testOutputHelper;
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
            _testOutputHelper.WriteLine($"EXCEPTION: {logRecord.Message}");
        }

        _testOutputHelper.WriteLine(message);
    }
}

public class XUnitLoggerFactory : ISyncLoggerFactory
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AsyncLocalContextValueProvider _contextValueProvider;

    public XUnitLoggerFactory(ITestOutputHelper testOutputHelper, AsyncLocalContextValueProvider contextValueProvider)
    {
        _testOutputHelper = testOutputHelper;
        _contextValueProvider = contextValueProvider;
    }

    public ISyncLogger CreateForCategory(string category)
    {
        return new XUnitLogger(_testOutputHelper, _contextValueProvider);
    }
}