namespace MX.Observability.ApplicationInsights.Jobs;

/// <summary>
/// Represents an in-progress job being tracked for telemetry.
/// </summary>
public interface IJobOperation : IAsyncDisposable
{
    /// <summary>Mark the job as successfully completed. Emits Audit:JobCompleted and a duration metric.</summary>
    void Complete(Dictionary<string, string>? additionalMetrics = null);

    /// <summary>Mark the job as failed. Emits Audit:JobFailed, exception telemetry, and flushes.</summary>
    Task FailAsync(Exception exception, Dictionary<string, string>? additionalProperties = null);
}
