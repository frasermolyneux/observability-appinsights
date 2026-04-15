namespace MX.Observability.ApplicationInsights.Auditing.Models;

/// <summary>
/// The result of the audited action.
/// </summary>
public enum AuditOutcome
{
    Success,
    Failure,
    Denied,
    Error
}
