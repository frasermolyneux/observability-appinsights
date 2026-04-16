using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Filtering;

/// <summary>
/// Telemetry module that registers the <see cref="TelemetryFilterProcessor"/> into the
/// Application Insights processor chain during SDK initialization.
/// <para>
/// The ASP.NET Core SDK discovers <see cref="ITelemetryModule"/> implementations registered
/// in DI and calls <see cref="Initialize"/> during <see cref="TelemetryConfiguration"/> setup.
/// This ensures the processor is added to the chain before it is built.
/// </para>
/// <para>
/// For Worker Service / Azure Functions, <c>AddApplicationInsightsTelemetryProcessor</c> handles
/// registration separately. Both paths may execute, but the <see cref="TelemetryFilterProcessor"/>
/// is idempotent — running through it twice produces the same result as running through it once
/// (items that should be filtered are dropped, items that should pass still pass).
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
