#pragma warning disable CS0618 // IHostingEnvironment is obsolete but still required by the Application Insights AspNetCore SDK's DI wiring.
using Microsoft.AspNetCore.Hosting;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using MX.Observability.ApplicationInsights.AspNetCore;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;
using MX.Observability.ApplicationInsights.Jobs;

namespace MX.Observability.ApplicationInsights.Tests.AspNetCore;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    private sealed class StubHostingEnvironment : IHostingEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

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
        services.AddSingleton<IHostingEnvironment>(new StubHostingEnvironment());
        services.AddApplicationInsightsTelemetry();
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
