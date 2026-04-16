using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Filtering;

/// <summary>
/// Telemetry module that registers the <see cref="TelemetryFilterProcessor"/> into the
/// Application Insights processor chain during SDK initialization.
/// <para>
/// Both ASP.NET Core and Worker Service SDKs discover <see cref="ITelemetryModule"/> implementations
/// registered in DI and call <see cref="Initialize"/> during <see cref="TelemetryConfiguration"/> setup.
/// This ensures the processor is added to the chain before it is built.
/// </para>
/// </summary>
internal sealed class TelemetryFilterModule : ITelemetryModule
{
    private readonly IOptionsMonitor<TelemetryFilterOptions> _optionsMonitor;

    public TelemetryFilterModule(IOptionsMonitor<TelemetryFilterOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public void Initialize(TelemetryConfiguration configuration)
    {
        configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
            .Use(next => new TelemetryFilterProcessor(next, _optionsMonitor));
    }
}
