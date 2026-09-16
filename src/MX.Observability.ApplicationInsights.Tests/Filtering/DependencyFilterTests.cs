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
    public void ShouldFilter_ConfiguredTableClientCacheMiss_ReturnsTrue()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            new ExpectedDependencyFailureOptions
            {
                Type = "InProc | Microsoft.Tables",
                Name = "TableClient.GetEntity",
                Target = "TableClient.GetEntity",
                MatchEmptyResultCode = true
            }
        ]);
        var dep = new DependencyTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(7),
            Type = "InProc | Microsoft.Tables",
            Name = "TableClient.GetEntity",
            Target = "TableClient.GetEntity",
            ResultCode = ""
        };

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_ConfiguredAzureTableCacheMiss_ReturnsTrue()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            CreateAzureTableCacheMissRule()
        ]);
        var dep = CreateAzureTableDependency("RepositoryCache", "404");

        Assert.True(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_UnrelatedAzureTableNotFound_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            CreateAzureTableCacheMissRule()
        ]);
        var dep = CreateAzureTableDependency("GameServerLiveStatus", "404");

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Theory]
    [InlineData("HTTP")]
    [InlineData("SQL")]
    [InlineData("Azure Blob")]
    public void ShouldFilter_UnrelatedDependencyTypeWith404_ReturnsFalse(string dependencyType)
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            CreateAzureTableCacheMissRule()
        ]);
        var dep = CreateAzureTableDependency("RepositoryCache", "404");
        dep.Type = dependencyType;

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Theory]
    [InlineData("429")]
    [InlineData("503")]
    [InlineData("500")]
    [InlineData("599")]
    [InlineData("408")]
    [InlineData("499")]
    [InlineData("TaskCanceledException")]
    [InlineData("Operation timed out")]
    public void ShouldFilter_CriticalFailureMatchingExpectedRule_ReturnsFalse(string resultCode)
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            new ExpectedDependencyFailureOptions
            {
                Type = "Azure table",
                NameSuffix = "/RepositoryCache",
                ResultCode = resultCode
            }
        ]);
        var dep = CreateAzureTableDependency("RepositoryCache", resultCode);

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_CancellationMarkerInProperties_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            new ExpectedDependencyFailureOptions
            {
                Type = "InProc | Microsoft.Tables",
                Name = "TableClient.GetEntity",
                MatchEmptyResultCode = true
            }
        ]);
        var dep = new DependencyTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(7),
            Type = "InProc | Microsoft.Tables",
            Name = "TableClient.GetEntity",
            ResultCode = ""
        };
        dep.Properties["error.type"] = "TaskCanceledException";

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_SlowExpectedFailure_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            CreateAzureTableCacheMissRule()
        ]);
        var dep = CreateAzureTableDependency("RepositoryCache", "404");
        dep.Duration = TimeSpan.FromMilliseconds(1001);

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_BlankResultFailureWithoutExactConfiguredOperation_ReturnsFalse()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            new ExpectedDependencyFailureOptions
            {
                Type = "InProc | Microsoft.Tables",
                Name = "TableClient.GetEntity",
                MatchEmptyResultCode = true
            }
        ]);
        var dep = new DependencyTelemetry
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(7),
            Type = "InProc | Microsoft.Tables",
            Name = "TableClient.UpsertEntity",
            ResultCode = ""
        };

        Assert.False(TelemetryFilterProcessor.ShouldFilterDependency(dep, rules));
    }

    [Fact]
    public void ShouldFilter_InvalidBroadExpectedFailureRule_IsIgnored()
    {
        var rules = CreateRules(o => o.Dependencies.ExpectedFailures =
        [
            new ExpectedDependencyFailureOptions
            {
                Type = "Azure table",
                ResultCode = "404"
            }
        ]);
        var dep = CreateAzureTableDependency("RepositoryCache", "404");

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

    private static ExpectedDependencyFailureOptions CreateAzureTableCacheMissRule() =>
        new()
        {
            Type = "Azure table",
            NamePrefix = "GET scache",
            NameSuffix = "/RepositoryCache",
            TargetPrefix = "scache",
            TargetContains = ".table.core.windows.net",
            ResultCode = "404"
        };

    private static DependencyTelemetry CreateAzureTableDependency(string tableName, string resultCode) =>
        new()
        {
            Success = false,
            Duration = TimeSpan.FromMilliseconds(7),
            Type = "Azure table",
            Name = $"GET scacheabc.table.core.windows.net/{tableName}",
            Target = "scacheabc.table.core.windows.net",
            ResultCode = resultCode
        };
}
