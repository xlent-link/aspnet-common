using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XlentLink.AspNet.Common.Context;
using XlentLink.AspNet.Common.Startup;

namespace XlentLink.AspNet.Common.Logging;

public class ApplicationInsightsLoggerFactory : ISyncLoggerFactory
{
    private readonly string _connectionString;
    private readonly IContextValueProvider _contextValueProvider;
    private readonly ISyncLogger _fallbackLogger;

    public ApplicationInsightsLoggerFactory(ComponentSettings componentSettings, IConfiguration configuration, IContextValueProvider contextValueProvider, ILoggerFactory fallbackLoggerFactory)
    {
        _fallbackLogger = new ConsoleLogger(fallbackLoggerFactory.CreateLogger("FallbackLogger"), contextValueProvider);

        string? connectionString = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(componentSettings.KeyVault?.Uri))
            {
                var client = new SecretClient(new Uri(componentSettings.KeyVault.Uri), new DefaultAzureCredential());
                var secret = client.GetSecret("TelemetryConfigurationConnectionString");
                connectionString = secret?.Value.Value;
            }
        }
        catch (Exception e)
        {
            _fallbackLogger.LogError($"Could not get TelemetryConfigurationConnectionString from Key Vault: {e.Message}", e);

            // Try a fall back value
            connectionString = configuration.GetSection("TelemetryConfiguration")["ConnectionString"];
        }

        _connectionString = connectionString ?? "";
        _contextValueProvider = contextValueProvider;
    }

    public ISyncLogger CreateForCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _fallbackLogger.LogError("There was no connection string for Application Insights, so falling back on Console logger");
            return _fallbackLogger;
        }

        return new ApplicationInsightsLogger(_connectionString, _contextValueProvider, category);
    }
}

public class ApplicationInsightsLogger : AbstractSyncLogger
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IContextValueProvider _contextValueProvider;
    private readonly string _category;

    public ApplicationInsightsLogger(string connectionString, IContextValueProvider contextValueProvider, string category)
    {
        _contextValueProvider = contextValueProvider;
        _category = category;
        _telemetryClient = new TelemetryClient(new TelemetryConfiguration
        {
            ApplicationIdProvider = new ApplicationInsightsApplicationIdProvider()
        });
        _telemetryClient = new TelemetryClient(new TelemetryConfiguration
        {
            ConnectionString = connectionString
        });
    }

    public override void LogSync(LogRecord logRecord)
    {
        if (logRecord.LogLevel == LogLevel.None) return;

        var properties = new Dictionary<string, string>
        {
            ["Timestamp"] = DateTimeOffset.Now.ToString("O"),
            ["Category"] = _category,
            ["LogLevel"] = logRecord.LogLevel.ToString(),
        };
        if (_contextValueProvider.Application != null) properties[nameof(_contextValueProvider.Application)] = _contextValueProvider.Application;
        if (_contextValueProvider.Environment != null) properties[nameof(_contextValueProvider.Environment)] = _contextValueProvider.Environment;
        if (_contextValueProvider.CorrelationId != null) properties[nameof(_contextValueProvider.CorrelationId)] = _contextValueProvider.CorrelationId;
        if (logRecord.Location != null) properties["Location"] = logRecord.Location;

        if (logRecord.Data != null)
        {
            foreach (var entry in logRecord.Data)
            {
                properties.TryAdd(entry.Key, JsonSerializer.Serialize(entry.Value));
            }
        }

        var severityLevel = logRecord.LogLevel switch
        {
            LogLevel.Critical => SeverityLevel.Critical,
            LogLevel.Error => SeverityLevel.Error,
            LogLevel.Warning => SeverityLevel.Warning,
            LogLevel.Information => SeverityLevel.Information,
            LogLevel.Trace => SeverityLevel.Verbose,
            LogLevel.Debug => SeverityLevel.Verbose,
            _ => SeverityLevel.Information
        };

        _telemetryClient.TrackTrace(logRecord.Message, severityLevel, properties);
        if (logRecord.Exception != null)
        {
            _telemetryClient.TrackException(logRecord.Exception, properties);
        }
    }
}
