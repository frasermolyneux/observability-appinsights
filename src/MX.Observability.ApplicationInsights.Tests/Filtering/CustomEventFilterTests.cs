using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Tests.Filtering;

[Trait("Category", "Unit")]
public class CustomEventFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            CustomEvents = new CustomEventFilterOptions
            {
                Enabled = true,
                AllowedNames = "",
                AllowedNamePrefixes = "Audit:"
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_CustomEventsDisabled_ReturnsFalse()
    {
        var rules = CreateRules(o => o.CustomEvents.Enabled = false);
        var evt = new EventTelemetry("AnyEvent");

        Assert.False(TelemetryFilterProcessor.ShouldFilterCustomEvent(evt, rules));
    }

    [Fact]
    public void ShouldFilter_AllowedPrefix_ReturnsFalse()
    {
        var rules = CreateRules();
        var evt = new EventTelemetry("Audit:AdminAction");

        Assert.False(TelemetryFilterProcessor.ShouldFilterCustomEvent(evt, rules));
    }

    [Fact]
    public void ShouldFilter_NotAllowedName_ReturnsTrue()
    {
        var rules = CreateRules();
        var evt = new EventTelemetry("PageView");

        Assert.True(TelemetryFilterProcessor.ShouldFilterCustomEvent(evt, rules));
    }

    [Fact]
    public void ShouldFilter_NoAllowListConfigured_ReturnsFalse()
    {
        var rules = CreateRules(o =>
        {
            o.CustomEvents.AllowedNames = "";
            o.CustomEvents.AllowedNamePrefixes = "";
        });
        var evt = new EventTelemetry("PageView");

        Assert.False(TelemetryFilterProcessor.ShouldFilterCustomEvent(evt, rules));
    }
}