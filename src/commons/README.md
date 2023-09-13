# Commons

~~Published as a nuget code library,~~
Used as Git submodule,
offers capability contracts and conventions for error handling, logging and other things.

## Correlation ids
Middleware for propagating the header "X-Correlation-ID".

## Exceptions
A set of sematic exceptions that can be translated into HTTP error codes.

- `ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`: Problem with an input argument. Yields a 400.
- `ResourceNotFoundException`: If a resource is missing. Yields a 404.
- `BusinessRuleException`: If a business rule was violated. Yields a 400.

## Error handling: ProblemDetails
No support for [ProblemDetails](https://tools.ietf.org/html/rfc7807) in .NET 7 at the time of writing the code,
so this is middleware for converting exceptions to Problem Detals output.

## Health checking
To enable a `/health` endpoint in a function app, first add this to `Program`:
```csharp
var healthChecks = services.AddHealthChecking();
healthChecks.AddCheck<YourIHealthCheck>("name");
```

then create a `HealthCheck` class that inherits `HealthFunctionBase`:
```csharp
using XlentLink.Commons.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Functions.Common;

public class HealthCheck : HealthFunctionBase
{
    public HealthCheck(HealthCheckService healthCheck) : base(healthCheck)
    {
    }
}
```


## Examples
`Program.cs` in a function app:
```csharp
var host = new HostBuilder()
    .ConfigureHostConfiguration(builder =>
    {
        builder.AddStandardAppSettingsProviders();
    })
    .ConfigureLogging((context, logging) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            logging.Services.TryAddSingleton<ISyncLoggerFactory, ConsoleLoggerFactory>();
            logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Trace);
        }
        else
        {
            logging.Services.TryAddSingleton<ISyncLoggerFactory, ApplicationInsightsLoggerFactory>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddStandardServicesAndSettings(context.Configuration);

        var adapterSettings = context.Configuration.GetSection(nameof(AdapterSettings)).Get<AdapterSettings>();
        services.AddSingleton(adapterSettings!);

        var componentSettings = services.BuildServiceProvider().GetRequiredService<ComponentSettings>();

        var healthChecks = services.AddHealthChecking(componentSettings, adapterSettings.TrackingDatabaseConnectionString);

        // Capability implementations
        services.TryAddSingleton<IConsignmentService, ConsignmentService>();
        services.TryAddSingleton<ITransportManagementCapability, TransportManagementCapability>();
        // ...
    })
    .ConfigureFunctionsWorkerDefaults((context, app) =>
    {
        // STANDARD MIDDLEWARES (correlation id propagation, problem details, etc)
        app.UseStandardMiddleware();
    })
    .Build();

host.Run();
```
