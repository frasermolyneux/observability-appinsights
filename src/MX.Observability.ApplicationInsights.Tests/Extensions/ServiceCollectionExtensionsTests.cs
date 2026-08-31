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
    public void AddObservabilityCore_WithoutConfiguration_BindsSafeDefaults()
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
