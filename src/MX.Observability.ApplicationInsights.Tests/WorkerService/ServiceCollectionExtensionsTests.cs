using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;
using MX.Observability.ApplicationInsights.Jobs;
using MX.Observability.ApplicationInsights.WorkerService;

namespace MX.Observability.ApplicationInsights.Tests.WorkerService;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    private sealed class NullTelemetryProcessor : ITelemetryProcessor
    {
        public void Process(ITelemetry item)
        {
        }
    }

    private static ServiceCollection CreateServicesWithApplicationInsights()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddApplicationInsightsTelemetryWorkerService();
        return services;
    }

    [Fact]
    public void AddObservability_RegistersCoreServices()
    {
        var services = CreateServicesWithApplicationInsights();

        services.AddObservability();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAuditLogger>());
        Assert.NotNull(provider.GetRequiredService<IJobTelemetry>());
        Assert.NotNull(provider.GetRequiredService<IOptions<TelemetryFilterOptions>>().Value);
    }

    [Fact]
    public void AddObservability_RegistersTelemetryFilterProcessorFactory()
    {
        var services = CreateServicesWithApplicationInsights();

        services.AddObservability();
        using var provider = services.BuildServiceProvider();

        var factory = Assert.Single(provider.GetServices<ITelemetryProcessorFactory>());
        var created = factory.Create(new NullTelemetryProcessor());

        Assert.IsType<TelemetryFilterProcessor>(created);
    }

    [Fact]
    public void AddObservability_WithConfigureFiltering_AppliesOverrides()
    {
        var services = CreateServicesWithApplicationInsights();

        services.AddObservability(options => options.Enabled = false);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<TelemetryFilterOptions>>().Value;

        Assert.False(options.Enabled);
    }
}
