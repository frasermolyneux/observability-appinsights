using Microsoft.ApplicationInsights.Channel;

namespace MX.Observability.ApplicationInsights.Tests.Helpers;

internal sealed class StubTelemetryChannel : ITelemetryChannel
{
    public Action<ITelemetry>? OnSend { get; set; }
    public bool? DeveloperMode { get; set; } = true;
    public string? EndpointAddress { get; set; }

    public void Dispose() { }
    public void Flush() { }
    public void Send(ITelemetry item) => OnSend?.Invoke(item);
}
