using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Tests.Filtering;

[Trait("Category", "Unit")]
public class RequestFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Requests = new RequestFilterOptions
            {
                Enabled = true,
                DurationThresholdMs = 1000,
                SuccessOnly = true,
                ExcludedPaths = "/healthz,/health",
                ExcludedHttpMethods = "",
                RetainedStatusCodes = "",
                RetainedStatusCodeRanges = ""
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_HealthCheckPath_AlwaysFiltered()
    {
        var rules = CreateRules();
        var req = new RequestTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(5000),
            ResponseCode = "500",
            Url = new Uri("https://example.com/healthz")
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_SuccessfulFastRequest_ReturnsTrue()
    {
        var rules = CreateRules();
        var req = new RequestTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            ResponseCode = "200",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_FailedRequest_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.RetainedStatusCodeRanges = "400-599");
        var req = new RequestTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(50),
            ResponseCode = "500",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_SlowRequest_ReturnsFalse()
    {
        var rules = CreateRules();
        var req = new RequestTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(2000),
            ResponseCode = "200",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_ExcludedHttpMethod_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Requests.ExcludedHttpMethods = "OPTIONS,HEAD");
        var req = new RequestTelemetry
        {
            Name = "OPTIONS /api/data",
            Success = false,
            Duration = TimeSpan.FromMilliseconds(5000),
            ResponseCode = "200",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_RetainedStatusCode_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.RetainedStatusCodes = "401,403");
        var req = new RequestTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            ResponseCode = "401",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }

    [Fact]
    public void ShouldFilter_DisabledRequestFilter_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Requests.Enabled = false);
        var req = new RequestTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            ResponseCode = "200",
            Url = new Uri("https://example.com/api/data")
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterRequest(req, rules));
    }
}
