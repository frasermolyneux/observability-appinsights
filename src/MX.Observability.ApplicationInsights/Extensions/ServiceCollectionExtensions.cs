using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;
using MX.Observability.ApplicationInsights.Jobs;

namespace MX.Observability.ApplicationInsights.Extensions;

/// <summary>
/// Extension methods for registering MX Observability services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all MX Observability services: telemetry filtering, audit logging, and job telemetry.
    /// Call after <c>AddApplicationInsightsTelemetry()</c> or <c>AddApplicationInsightsTelemetryWorkerService()</c>.
    /// <para>
    /// The telemetry filter processor is registered via <see cref="ITelemetryModule"/>, which is the
    /// official SDK extension point. Both ASP.NET Core and Worker Service SDKs discover and initialize
    /// <see cref="ITelemetryModule"/> implementations from DI during <see cref="TelemetryConfiguration"/> setup.
    /// </para>
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName);

        services.AddSingleton<ITelemetryModule, TelemetryFilterModule>();
        services.AddSingleton<IAuditLogger, ApplicationInsightsAuditLogger>();
        services.AddSingleton<IJobTelemetry, ApplicationInsightsJobTelemetry>();

        return services;
    }

    /// <summary>
    /// Registers all MX Observability services with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName)
            .Configure(configureFiltering);

        services.AddSingleton<ITelemetryModule, TelemetryFilterModule>();
        services.AddSingleton<IAuditLogger, ApplicationInsightsAuditLogger>();
        services.AddSingleton<IJobTelemetry, ApplicationInsightsJobTelemetry>();

        return services;
    }

    /// <summary>
    /// Registers the audit logger for structured event emission.
    /// </summary>
    public static IServiceCollection AddAuditLogging(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLogger, ApplicationInsightsAuditLogger>();
        return services;
    }

    /// <summary>
    /// Registers the job telemetry service for scheduled job lifecycle tracking.
    /// </summary>
    public static IServiceCollection AddJobTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IJobTelemetry, ApplicationInsightsJobTelemetry>();
        return services;
    }
}
