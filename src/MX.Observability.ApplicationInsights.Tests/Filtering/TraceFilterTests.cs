using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Tests.Filtering;

[Trait("Category", "Unit")]
public class TraceFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Traces = new TraceFilterOptions
            {
                Enabled = true,
                MinSeverity = "Warning",
                AlwaysRetainCategories = "",
                ExcludedCategories = "",
                ExcludedMessageContains = ""
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_BelowMinSeverity_ReturnsTrue()
    {
        var rules = CreateRules();
        var trace = new TraceTelemetry("info message", SeverityLevel.Information);

        Assert.True(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_AtMinSeverity_ReturnsFalse()
    {
        var rules = CreateRules();
        var trace = new TraceTelemetry("warning message", SeverityLevel.Warning);

        Assert.False(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_AboveMinSeverity_ReturnsFalse()
    {
        var rules = CreateRules();
        var trace = new TraceTelemetry("error message", SeverityLevel.Error);

        Assert.False(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_ExcludedCategory_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Traces.ExcludedCategories = "Microsoft.AspNetCore,System.Net.Http");
        var trace = new TraceTelemetry("request started", SeverityLevel.Critical);
        trace.Properties["CategoryName"] = "Microsoft.AspNetCore";

        Assert.True(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_RetainedCategory_NeverFiltered()
    {
        var rules = CreateRules(o => o.Traces.AlwaysRetainCategories = "MyApp.Critical");
        var trace = new TraceTelemetry("verbose retained", SeverityLevel.Verbose);
        trace.Properties["CategoryName"] = "MyApp.Critical";

        Assert.False(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_ExcludedMessageSubstring_Filtered()
    {
        var rules = CreateRules(o => o.Traces.ExcludedMessageContains = "heartbeat,ping");
        var trace = new TraceTelemetry("Sending heartbeat to server", SeverityLevel.Critical);

        Assert.True(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }

    [Fact]
    public void ShouldFilter_DisabledTraceFilter_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Traces.Enabled = false);
        var trace = new TraceTelemetry("verbose message", SeverityLevel.Verbose);

        Assert.False(TelemetryFilterProcessor.ShouldFilterTrace(trace, rules));
    }
}
