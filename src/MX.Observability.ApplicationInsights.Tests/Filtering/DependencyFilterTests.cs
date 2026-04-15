using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Tests.Filtering;

[Trait("Category", "Unit")]
public class DependencyFilterTests
{
    private static ParsedFilterRules CreateRules(Action<TelemetryFilterOptions>? configure = null)
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Dependencies = new DependencyFilterOptions
            {
                Enabled = true,
                FilterAllTypes = true,
                DurationThresholdMs = 1000
            }
        };
        configure?.Invoke(options);
        return ParsedFilterRules.From(options);
    }

    [Fact]
    public void ShouldFilter_SuccessfulFastDependency_FilterAllTypes_ReturnsTrue()
    {
        var rules = CreateRules();
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "HTTP"
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_FailedDependency_ReturnsFalse()
    {
        var rules = CreateRules();
        var dep = new DependencyTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "HTTP"
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_SlowDependency_ReturnsFalse()
    {
        var rules = CreateRules();
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(2000),
            Type = "HTTP"
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_IgnoredTarget_AlwaysFiltered()
    {
        var rules = CreateRules(o => o.Dependencies.IgnoredTargets = "localhost,127.0.0.1");
        var dep = new DependencyTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(5000),
            Target = "localhost",
            Type = "HTTP"
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_RetainedResultCode_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.RetainedResultCodes = "429,503");
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            ResultCode = "429",
            Type = "HTTP"
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_DisabledDependencyFilter_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.Enabled = false);
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "HTTP"
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeNotInExcludedList_WhenNotFilterAll_ReturnsFalse()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypes = "SQL,Azure Table";
        });
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "HTTP"
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeInExcludedList_WhenNotFilterAll_ReturnsTrue()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypes = "SQL,Azure Table";
        });
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "SQL"
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_TypeMatchesPrefixExclusion_ReturnsTrue()
    {
        var rules = CreateRules(o =>
        {
            o.Dependencies.FilterAllTypes = false;
            o.Dependencies.ExcludedTypePrefixes = "Azure";
        });
        var dep = new DependencyTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            Type = "Azure Blob"
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }
}
