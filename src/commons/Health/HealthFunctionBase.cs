using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace XlentLink.AspNet.Common.Health;

/// <summary>
/// Inherit from this class to setup health checking in your function app
/// </summary>
public class HealthFunctionBase
{
    private readonly Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService _healthCheck;

    private static readonly JsonObjectSerializer Serializer = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    });

    /// <summary>
    /// Note: Use <see cref="HealthExtensions.AddHealthChecking"/> to setup dependency injection.
    /// </summary>
    public HealthFunctionBase(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
    }

    [Function("HealthCheck")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "health")] HttpRequestData req)
    {
        var healthReport = await _healthCheck.CheckHealthAsync();

        var responseCode = healthReport.Status switch
        {
            HealthStatus.Unhealthy => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.OK
        };
        var response = req.CreateResponse(responseCode);
        await response.WriteAsJsonAsync(new HealthResponse
        {
            Status = healthReport.Status,
            TotalDuration = healthReport.TotalDuration,
            Entries = healthReport.Entries.ToDictionary(_ => _.Key, _ => new HealthResponseEntry
            {
                Status = _.Value.Status,
                Description = _.Value.Description,
                Duration = _.Value.Duration,
                Data = _.Value.Data.Any() ? _.Value.Data : null,
                Tags = _.Value.Tags.Any() ? _.Value.Tags : null,
                ExceptionMessage = _.Value.Exception?.Message, // Note: this whole Entries.Select thing is because Exception cannot be serialized
            })
        }, serializer: Serializer);
        return response;
    }
}