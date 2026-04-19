using Microsoft.Extensions.DependencyInjection;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Filtering.Configuration;
using MX.Observability.ApplicationInsights.Jobs;

namespace MX.Observability.ApplicationInsights.Extensions;

/// <summary>
/// Extension methods for registering MX Observability core services.
/// <para>
/// This package contains the hosting-agnostic pieces: filter options binding, the
/// <see cref="MX.Observability.ApplicationInsights.Filtering.TelemetryFilterProcessor"/> implementation,
/// audit logging and job telemetry. Consumers should normally reference one of the host-specific
/// adapter packages (<c>MX.Observability.ApplicationInsights.AspNetCore</c> or
/// <c>MX.Observability.ApplicationInsights.WorkerService</c>) which call <see cref="AddObservabilityCore"/>
/// internally and additionally wire the telemetry processor into the correct SDK pipeline.
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the hosting-agnostic MX Observability services: telemetry filter options,
    /// audit logging and job telemetry. Does NOT wire the telemetry processor into the
    /// Application Insights pipeline — use a host-specific adapter package for that.
    /// </summary>
    public static IServiceCollection AddObservabilityCore(this IServiceCollection services)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName);

        services.AddSingleton<IAuditLogger, ApplicationInsightsAuditLogger>();
        services.AddSingleton<IJobTelemetry, ApplicationInsightsJobTelemetry>();

        return services;
    }

    /// <summary>
    /// Registers the hosting-agnostic MX Observability services with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservabilityCore(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName)
            .Configure(configureFiltering);

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
