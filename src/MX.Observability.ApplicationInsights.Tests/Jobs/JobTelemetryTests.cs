using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Auditing.Models;
using MX.Observability.ApplicationInsights.Jobs;
using MX.Observability.ApplicationInsights.Tests.Helpers;

namespace MX.Observability.ApplicationInsights.Tests.Jobs;

[Trait("Category", "Unit")]
public class JobTelemetryTests
{
    private readonly List<AuditEvent> _auditEvents = new();
    private readonly List<ITelemetry> _sentTelemetry = new();
    private readonly ApplicationInsightsJobTelemetry _jobTelemetry;

    public JobTelemetryTests()
    {
        var channel = new StubTelemetryChannel { OnSend = t => _sentTelemetry.Add(t) };
        var config = new TelemetryConfiguration { TelemetryChannel = channel };
        var client = new TelemetryClient(config);
        var auditLogger = new StubAuditLogger(_auditEvents);
        _jobTelemetry = new ApplicationInsightsJobTelemetry(auditLogger, client);
    }

    [Fact]
    public async Task ExecuteAsync_Success_EmitsStartAndCompleteEvents()
    {
        await _jobTelemetry.ExecuteAsync("TestJob", () => Task.CompletedTask);

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobCompleted", _auditEvents[1].EventName);
    }

    [Fact]
    public async Task ExecuteAsync_Failure_EmitsStartAndFailEvents()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobTelemetry.ExecuteAsync("TestJob", () =>
                throw new InvalidOperationException("test error")));

        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobFailed", _auditEvents[1].EventName);
        Assert.Equal(AuditOutcome.Error, _auditEvents[1].Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Failure_TracksException()
    {
        var ex = new InvalidOperationException("test error");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobTelemetry.ExecuteAsync("TestJob", () => throw ex));

        var exceptionTelemetry = _sentTelemetry.OfType<ExceptionTelemetry>().SingleOrDefault();
        Assert.NotNull(exceptionTelemetry);
        Assert.Equal(ex, exceptionTelemetry.Exception);
    }

    [Fact]
    public void StartJob_Complete_EmitsDurationMetric()
    {
        var operation = _jobTelemetry.StartJob("MetricJob");
        operation.Complete();

        var metric = _sentTelemetry.OfType<MetricTelemetry>().SingleOrDefault();
        Assert.NotNull(metric);
        Assert.Equal("MetricJob_Duration", metric.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_ReturnsResult()
    {
        var result = await _jobTelemetry.ExecuteAsync("TestJob", () => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(2, _auditEvents.Count);
        Assert.Equal("JobStarted", _auditEvents[0].EventName);
        Assert.Equal("JobCompleted", _auditEvents[1].EventName);
    }

    private sealed class StubAuditLogger : IAuditLogger
    {
        private readonly List<AuditEvent> _events;

        public StubAuditLogger(List<AuditEvent> events) => _events = events;

        public void LogAudit(AuditEvent auditEvent) => _events.Add(auditEvent);
    }
}
