using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Moq;
using MX.Observability.ApplicationInsights.Filtering;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Tests.Filtering;

[Trait("Category", "Unit")]
public class TelemetryFilterProcessorTests
{
    private readonly List<ITelemetry> _passedThrough = new();
    private readonly Mock<ITelemetryProcessor> _nextProcessor = new();

    public TelemetryFilterProcessorTests()
    {
        _nextProcessor.Setup(p => p.Process(It.IsAny<ITelemetry>()))
            .Callback<ITelemetry>(t => _passedThrough.Add(t));
    }

    private TelemetryFilterProcessor CreateProcessor(TelemetryFilterOptions? options = null)
    {
        options ??= new TelemetryFilterOptions();
        var monitor = Mock.Of<IOptionsMonitor<TelemetryFilterOptions>>(m =>
            m.CurrentValue == options);
        return new TelemetryFilterProcessor(_nextProcessor.Object, monitor);
    }

    [Fact]
    public void Process_WhenDisabled_PassesAllTelemetryThrough()
    {
        var options = new TelemetryFilterOptions { Enabled = false };
        var processor = CreateProcessor(options);

        var dep = new DependencyTelemetry { Success = true, Duration = TimeSpan.FromMilliseconds(1) };
        var req = new RequestTelemetry { Success = true, Duration = TimeSpan.FromMilliseconds(1), ResponseCode = "200" };
        var trace = new TraceTelemetry("test", SeverityLevel.Verbose);

        processor.Process(dep);
        processor.Process(req);
        processor.Process(trace);

        Assert.Equal(3, _passedThrough.Count);
    }

    [Fact]
    public void Process_DependencyTelemetry_DelegatesToShouldFilter()
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
        var processor = CreateProcessor(options);

        var dep = new DependencyTelemetry { Success = true, Duration = TimeSpan.FromMilliseconds(50) };
        processor.Process(dep);

        Assert.Empty(_passedThrough);
    }

    [Fact]
    public void Process_RequestTelemetry_DelegatesToShouldFilter()
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Requests = new RequestFilterOptions
            {
                Enabled = true,
                ExcludedPaths = "/health",
                DurationThresholdMs = 1000,
                RetainedStatusCodeRanges = ""
            }
        };
        var processor = CreateProcessor(options);

        var req = new RequestTelemetry
        {
            Success = true,
            Duration = TimeSpan.FromMilliseconds(50),
            ResponseCode = "200",
            Url = new Uri("https://example.com/health")
        };
        processor.Process(req);

        Assert.Empty(_passedThrough);
    }

    [Fact]
    public void Process_TraceTelemetry_DelegatesToShouldFilter()
    {
        var options = new TelemetryFilterOptions
        {
            Enabled = true,
            Traces = new TraceFilterOptions
            {
                Enabled = true,
                MinSeverity = "Warning"
            }
        };
        var processor = CreateProcessor(options);

        var trace = new TraceTelemetry("debug message", SeverityLevel.Verbose);
        processor.Process(trace);

        Assert.Empty(_passedThrough);
    }

    [Fact]
    public void Process_ExceptionTelemetry_AlwaysPassesThrough()
    {
        var processor = CreateProcessor();
        var exception = new ExceptionTelemetry(new InvalidOperationException("test"));

        processor.Process(exception);

        Assert.Single(_passedThrough);
        Assert.IsType<ExceptionTelemetry>(_passedThrough[0]);
    }

    [Fact]
    public void Process_EventTelemetry_AlwaysPassesThrough()
    {
        var processor = CreateProcessor();
        var evt = new EventTelemetry("TestEvent");

        processor.Process(evt);

        Assert.Single(_passedThrough);
        Assert.IsType<EventTelemetry>(_passedThrough[0]);
    }

    [Fact]
    public void Process_MetricTelemetry_AlwaysPassesThrough()
    {
        var processor = CreateProcessor();
        var metric = new MetricTelemetry("TestMetric", 42);

        processor.Process(metric);

        Assert.Single(_passedThrough);
        Assert.IsType<MetricTelemetry>(_passedThrough[0]);
    }
}
