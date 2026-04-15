using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Auditing.Models;

namespace MX.Observability.ApplicationInsights.Auditing;

/// <summary>
/// Application Insights implementation of <see cref="IAuditLogger"/>.
/// Emits audit events as custom events with "Audit:" prefix and standardised property keys.
/// </summary>
public sealed class ApplicationInsightsAuditLogger : IAuditLogger
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsAuditLogger(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public void LogAudit(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var telemetry = new EventTelemetry($"Audit:{auditEvent.EventName}");

        // Standard properties — always present for consistent KQL querying
        telemetry.Properties["Audit.Category"] = auditEvent.Category.ToString();
        telemetry.Properties["Audit.Action"] = auditEvent.Action.ToString();
        telemetry.Properties["Audit.Outcome"] = auditEvent.Outcome.ToString();
        telemetry.Properties["Audit.ActorType"] = auditEvent.ActorType.ToString();

        SetIfNotNull(telemetry, "Audit.ActorId", auditEvent.ActorId);
        SetIfNotNull(telemetry, "Audit.ActorName", auditEvent.ActorName);
        SetIfNotNull(telemetry, "Audit.TargetId", auditEvent.TargetId);
        SetIfNotNull(telemetry, "Audit.TargetType", auditEvent.TargetType);
        SetIfNotNull(telemetry, "Audit.TargetName", auditEvent.TargetName);
        SetIfNotNull(telemetry, "Audit.SourceComponent", auditEvent.SourceComponent);
        SetIfNotNull(telemetry, "Audit.CorrelationId", auditEvent.CorrelationId);

        // Consumer-provided extensible properties
        foreach (var (key, value) in auditEvent.Properties)
            telemetry.Properties[key] = value;

        _telemetryClient.TrackEvent(telemetry);
    }

    private static void SetIfNotNull(EventTelemetry telemetry, string key, string? value)
    {
        if (value is not null)
            telemetry.Properties[key] = value;
    }
}
