using Microsoft.Extensions.DependencyInjection;
using MX.Observability.ApplicationInsights.Extensions;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.AspNetCore;

/// <summary>
/// ASP.NET Core registration entry point for MX Observability.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MX Observability for an ASP.NET Core host.
    /// <para>
    /// Call this after <c>services.AddApplicationInsightsTelemetry()</c>. This method:
    /// </para>
    /// <list type="bullet">
    ///   <item>Binds <see cref="TelemetryFilterOptions"/> from the <c>MX:Observability:Filtering</c> configuration section.</item>
    ///   <item>Registers <see cref="MX.Observability.ApplicationInsights.Auditing.IAuditLogger"/> and
    ///         <see cref="MX.Observability.ApplicationInsights.Jobs.IJobTelemetry"/>.</item>
    ///   <item>Registers <see cref="TelemetryFilterProcessor"/> via the SDK's
    ///         <c>AddApplicationInsightsTelemetryProcessor&lt;T&gt;()</c>, which inserts it into the
    ///         live processor chain when the Application Insights SDK builds the active
    ///         <see cref="Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration"/>.
    ///         Constructor parameters are resolved from DI via <c>ActivatorUtilities</c>.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddObservabilityCore();
        services.AddApplicationInsightsTelemetryProcessor<TelemetryFilterProcessor>();
        return services;
    }

    /// <summary>
    /// Registers MX Observability for an ASP.NET Core host with custom filter configuration.
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        Action<TelemetryFilterOptions> configureFiltering)
    {
        services.AddObservabilityCore(configureFiltering);
        services.AddApplicationInsightsTelemetryProcessor<TelemetryFilterProcessor>();
        return services;
    }
}
