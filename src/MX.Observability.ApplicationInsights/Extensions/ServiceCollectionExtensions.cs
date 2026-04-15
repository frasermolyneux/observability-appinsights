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
    /// Registers the telemetry filter processor with configuration binding.
    /// Call after AddApplicationInsightsTelemetry() or AddApplicationInsightsTelemetryWorkerService().
    /// </summary>
    public static IServiceCollection AddTelemetryFiltering(
        this IServiceCollection services,
        string configSection = TelemetryFilterOptions.SectionName)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(configSection);

        services.AddApplicationInsightsTelemetryProcessor<TelemetryFilterProcessor>();

        return services;
    }

    /// <summary>
    /// Registers the telemetry filter processor with an action to override defaults.
    /// </summary>
    public static IServiceCollection AddTelemetryFiltering(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configure)
    {
        services.AddOptions<TelemetryFilterOptions>()
            .BindConfiguration(TelemetryFilterOptions.SectionName)
            .Configure(configure);

        services.AddApplicationInsightsTelemetryProcessor<TelemetryFilterProcessor>();

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
    /// Requires <see cref="AddAuditLogging"/> to be called first (or use <see cref="AddObservability"/>).
    /// </summary>
    public static IServiceCollection AddJobTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IJobTelemetry, ApplicationInsightsJobTelemetry>();
        return services;
    }

    /// <summary>
    /// Registers all MX Observability services: telemetry filtering, audit logging, and job telemetry.
    /// Call after AddApplicationInsightsTelemetry() or AddApplicationInsightsTelemetryWorkerService().
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddTelemetryFiltering();
        services.AddAuditLogging();
        services.AddJobTelemetry();
        return services;
    }

    /// <summary>
    /// Registers all MX Observability services with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        services.AddTelemetryFiltering(configureFiltering);
        services.AddAuditLogging();
        services.AddJobTelemetry();
        return services;
    }
}
