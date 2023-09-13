using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace XlentLink.Commons.Health;

/// <summary>
/// A mimic of the standard <see cref="HealthReport"/>, so that we can serialize it.
/// </summary>
public class HealthResponse
{
    public HealthStatus Status { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public IDictionary<string, HealthResponseEntry>? Entries { get; set; }
}

public class HealthResponseEntry
{
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
    public IEnumerable<string>? Tags { get; set; }
    public string? ExceptionMessage { get; set; }
}