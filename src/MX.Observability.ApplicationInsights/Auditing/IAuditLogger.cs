using MX.Observability.ApplicationInsights.Auditing.Models;

namespace MX.Observability.ApplicationInsights.Auditing;

/// <summary>
/// Emits structured audit events to Application Insights as custom events
/// with standardised properties for consistent cross-application querying.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event. All events are emitted with an "Audit:" prefix
    /// and consistent "Audit.*" property keys.
    /// </summary>
    void LogAudit(AuditEvent auditEvent);
}
