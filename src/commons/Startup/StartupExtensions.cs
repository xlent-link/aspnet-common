using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using XlentLink.AspNet.Common.Context;
using XlentLink.AspNet.Common.CorrelationId;
using XlentLink.AspNet.Common.Exceptions;
using XlentLink.AspNet.Common.ProblemDetails;

namespace XlentLink.AspNet.Common.Startup;

public static class StartupExtensions
{
    /// <summary>
    /// Adds a set of app settings providers that are commonly used.
    /// </summary>
    public static IConfigurationBuilder AddStandardAppSettingsProviders(this IConfigurationBuilder builder)
    {
        builder
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("local.settings.json", true, true);
        return builder;
    }

    /// <summary>
    /// Adds a set of services and settings that are commonly used.
    /// </summary>
    public static IServiceCollection AddStandardServicesAndSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var componentSettings = configuration.GetSection("ComponentSettings").Get<ComponentSettings>();
        if (componentSettings == null) throw new AssertionException("Could not find app setting section 'ComponentSettings'.");

        var contextValueProvider = new AsyncLocalContextValueProvider
        {
            Application = componentSettings.ApplicationName,
            Environment = componentSettings.Environment,
        };
        services.TryAddSingleton<IContextValueProvider>(_ => contextValueProvider);

        services.TryAddSingleton<ComponentSettings>(componentSettings);
        services.TryAddSingleton<ProblemDetailsOptions>(new ProblemDetailsOptions
        {
            ApplicationUrnPart = $"{componentSettings.ApplicationName}:{componentSettings.Environment}"
        });

        return services;
    }

    public static void UseStandardMiddleware(this IFunctionsWorkerApplicationBuilder app)
    {
        app.UseMiddleware<CreateCorrelationIdIfMissing>(); // Should be one of the first
        app.UseMiddleware<ProblemDetailsMiddleware>();     // Depends on correlation id
    }
}