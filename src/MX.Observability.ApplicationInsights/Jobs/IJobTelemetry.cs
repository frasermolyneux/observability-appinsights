namespace MX.Observability.ApplicationInsights.Jobs;

/// <summary>
/// Tracks the lifecycle of scheduled or background jobs with automatic audit event emission
/// and duration metrics.
/// </summary>
public interface IJobTelemetry
{
    /// <summary>
    /// Starts tracking a job. Returns an operation that must be completed or failed.
    /// Emits an Audit:JobStarted event.
    /// </summary>
    IJobOperation StartJob(string jobName, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Convenience wrapper — executes the action with automatic start/complete/fail tracking.
    /// </summary>
    Task<T> ExecuteAsync<T>(string jobName, Func<Task<T>> action, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Convenience wrapper — executes the action with automatic start/complete/fail tracking.
    /// </summary>
    Task ExecuteAsync(string jobName, Func<Task> action, Dictionary<string, string>? properties = null);
}
