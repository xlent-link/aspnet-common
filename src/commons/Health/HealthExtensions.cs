using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using XlentLink.AspNet.Common.Startup;

namespace XlentLink.AspNet.Common.Health;

public static class HealthExtensions
{
    /// <summary>
    /// Sets up health checking.
    ///
    /// Use the <see cref="IHealthChecksBuilder"/> to add your own health checks.
    ///
    /// Has convenience for probing a database.
    /// </summary>
    /// <remarks>
    /// Make sure to inherit <see cref="HealthFunctionBase"/> to create the health endpoint
    /// </remarks>
    public static IHealthChecksBuilder AddHealthChecking(this IServiceCollection services, ComponentSettings componentSettings, string? databaseConnectionString = null)
    {
        var healthChecks = services.AddHealthChecks();

        healthChecks.AddCheck("component", _ => HealthCheckResult.Healthy(),
            new[]
            {
                $"ApplicationName: {componentSettings.ApplicationName}",
                $"Environment: {componentSettings.Environment}",
            });

        if (databaseConnectionString != null)
        {
            healthChecks.AddSqlServer(databaseConnectionString, timeout: TimeSpan.FromSeconds(2));
        }

        return healthChecks;
    }
}