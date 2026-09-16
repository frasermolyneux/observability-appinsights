using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Extensions;
using MX.Observability.ApplicationInsights.Filtering.Configuration;
using MX.Observability.ApplicationInsights.Jobs;

namespace MX.Observability.ApplicationInsights.Tests.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));
        return services;
    }

    [Fact]
    public void AddObservabilityCore_RegistersAuditLoggerAndJobTelemetry()
    {
        var services = CreateServicesWithConfiguration();

        services.AddObservabilityCore();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAuditLogger>());
        Assert.NotNull(provider.GetRequiredService<IJobTelemetry>());
    }

    [Fact]
    public void AddObservabilityCore_WithEmptyConfiguration_BindsSafeDefaults()
    {
        var services = CreateServicesWithConfiguration();

        services.AddObservabilityCore();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TelemetryFilterOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.NotNull(options.Dependencies);
        Assert.NotNull(options.Requests);
        Assert.NotNull(options.Traces);
        Assert.NotNull(options.CustomEvents);
    }

    [Fact]
    public void AddObservabilityCore_WithConfigureFiltering_AppliesOverrides()
    {
        var services = CreateServicesWithConfiguration();

        services.AddObservabilityCore(options => options.Enabled = false);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TelemetryFilterOptions>>().Value;

        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddObservabilityCore_BindsExpectedDependencyFailures()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:0:Type"] = "Azure table",
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:0:NameSuffix"] = "/RepositoryCache",
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:0:ResultCode"] = "404",
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:1:Type"] = "InProc | Microsoft.Tables",
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:1:Name"] = "TableClient.GetEntity",
                ["ApplicationInsights:TelemetryFilter:Dependencies:ExpectedFailures:1:MatchEmptyResultCode"] = "true"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));

        services.AddObservabilityCore();
        using var provider = services.BuildServiceProvider();

        var expectedFailures = provider.GetRequiredService<IOptions<TelemetryFilterOptions>>()
            .Value.Dependencies.ExpectedFailures;

        Assert.Collection(
            expectedFailures,
            rule =>
            {
                Assert.Equal("Azure table", rule.Type);
                Assert.Equal("/RepositoryCache", rule.NameSuffix);
                Assert.Equal("404", rule.ResultCode);
            },
            rule =>
            {
                Assert.Equal("InProc | Microsoft.Tables", rule.Type);
                Assert.Equal("TableClient.GetEntity", rule.Name);
                Assert.True(rule.MatchEmptyResultCode);
            });
    }

    [Fact]
    public void AddAuditLogging_RegistersAuditLoggerOnly()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));

        services.AddAuditLogging();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAuditLogger>());
        Assert.Null(provider.GetService<IJobTelemetry>());
    }

    [Fact]
    public void AddJobTelemetry_RegistersJobTelemetryOnly()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));
        services.AddSingleton(Mock.Of<IAuditLogger>());

        services.AddJobTelemetry();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IJobTelemetry>());
    }

    [Fact]
    public void AddObservabilityCore_RegistersAuditLoggerAndJobTelemetryAsSingletons()
    {
        var services = CreateServicesWithConfiguration();

        services.AddObservabilityCore();
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<IAuditLogger>(), provider.GetRequiredService<IAuditLogger>());
        Assert.Same(provider.GetRequiredService<IJobTelemetry>(), provider.GetRequiredService<IJobTelemetry>());
    }
}
