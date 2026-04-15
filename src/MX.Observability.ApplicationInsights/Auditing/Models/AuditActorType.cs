namespace MX.Observability.ApplicationInsights.Auditing.Models;

/// <summary>
/// Identifies who or what performed the action.
/// </summary>
public enum AuditActorType
{
    /// <summary>An authenticated human user.</summary>
    User,
    /// <summary>An automated system process.</summary>
    System,
    /// <summary>An external or internal service.</summary>
    Service
}
