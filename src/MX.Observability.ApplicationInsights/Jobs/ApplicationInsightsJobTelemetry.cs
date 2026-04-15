using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Auditing.Models;

namespace MX.Observability.ApplicationInsights.Jobs;

/// <summary>
/// Application Insights implementation of <see cref="IJobTelemetry"/>.
/// Integrates with <see cref="IAuditLogger"/> for audit trail and emits duration metrics.
/// </summary>
public sealed class ApplicationInsightsJobTelemetry : IJobTelemetry
{
    private readonly IAuditLogger _auditLogger;
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsJobTelemetry(IAuditLogger auditLogger, TelemetryClient telemetryClient)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public IJobOperation StartJob(string jobName, Dictionary<string, string>? properties = null)
    {
        var operation = new JobOperation(_auditLogger, _telemetryClient, jobName, properties);
        operation.TrackStart();
        return operation;
    }

    public async Task<T> ExecuteAsync<T>(string jobName, Func<Task<T>> action, Dictionary<string, string>? properties = null)
    {
        var operation = StartJob(jobName, properties);
        try
        {
            var result = await action().ConfigureAwait(false);
            operation.Complete();
            return result;
        }
        catch (Exception ex)
        {
            await operation.FailAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    public async Task ExecuteAsync(string jobName, Func<Task> action, Dictionary<string, string>? properties = null)
    {
        var operation = StartJob(jobName, properties);
        try
        {
            await action().ConfigureAwait(false);
            operation.Complete();
        }
        catch (Exception ex)
        {
            await operation.FailAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private sealed class JobOperation : IJobOperation
    {
        private readonly IAuditLogger _auditLogger;
        private readonly TelemetryClient _telemetryClient;
        private readonly string _jobName;
        private readonly Dictionary<string, string> _properties;
        private readonly Stopwatch _stopwatch = new();
        private bool _completed;

        public JobOperation(IAuditLogger auditLogger, TelemetryClient telemetryClient, string jobName, Dictionary<string, string>? properties)
        {
            _auditLogger = auditLogger;
            _telemetryClient = telemetryClient;
            _jobName = jobName;
            _properties = properties is not null ? new Dictionary<string, string>(properties) : new();
        }

        public void TrackStart()
        {
            _stopwatch.Start();
            _auditLogger.LogAudit(AuditEvent.SystemAction("JobStarted", AuditAction.Execute)
                .WithService(_jobName)
                .WithSource(_jobName)
                .WithProperties(_properties)
                .Build());
        }

        public void Complete(Dictionary<string, string>? additionalMetrics = null)
        {
            if (_completed) return;
            _completed = true;
            _stopwatch.Stop();

            var builder = AuditEvent.SystemAction("JobCompleted", AuditAction.Execute)
                .WithService(_jobName)
                .WithSource(_jobName)
                .WithProperty("DurationMs", _stopwatch.ElapsedMilliseconds.ToString())
                .WithProperties(_properties);

            if (additionalMetrics is not null)
                builder.WithProperties(additionalMetrics);

            _auditLogger.LogAudit(builder.Build());

            var metric = new MetricTelemetry($"{_jobName}_Duration", _stopwatch.ElapsedMilliseconds);
            foreach (var (key, value) in _properties)
                metric.Properties[key] = value;
            _telemetryClient.TrackMetric(metric);
        }

        public async Task FailAsync(Exception exception, Dictionary<string, string>? additionalProperties = null)
        {
            if (_completed) return;
            _completed = true;
            _stopwatch.Stop();

            var builder = AuditEvent.SystemAction("JobFailed", AuditAction.Execute)
                .WithService(_jobName)
                .WithSource(_jobName)
                .WithOutcome(AuditOutcome.Error)
                .WithProperty("DurationMs", _stopwatch.ElapsedMilliseconds.ToString())
                .WithProperty("ExceptionType", exception.GetType().Name)
                .WithProperty("ExceptionMessage", exception.Message)
                .WithProperties(_properties);

            if (additionalProperties is not null)
                builder.WithProperties(additionalProperties);

            _auditLogger.LogAudit(builder.Build());

            var exceptionProperties = new Dictionary<string, string>(_properties)
            {
                ["JobName"] = _jobName,
                ["DurationMs"] = _stopwatch.ElapsedMilliseconds.ToString()
            };
            if (additionalProperties is not null)
            {
                foreach (var (key, value) in additionalProperties)
                    exceptionProperties[key] = value;
            }
            _telemetryClient.TrackException(exception, exceptionProperties);
            await _telemetryClient.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
            if (!_completed)
                Complete();
            return ValueTask.CompletedTask;
        }
    }
}
