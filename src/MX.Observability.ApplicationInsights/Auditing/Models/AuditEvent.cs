namespace MX.Observability.ApplicationInsights.Auditing.Models;

/// <summary>
/// Represents a structured audit event emitted to Application Insights.
/// Use the factory methods (<see cref="UserAction"/>, <see cref="ServerAction"/>, <see cref="SystemAction"/>)
/// to create events with the correct category and actor type.
/// </summary>
public sealed class AuditEvent
{
    public string EventName { get; init; } = "";
    public AuditCategory Category { get; init; }
    public AuditAction Action { get; init; }
    public AuditOutcome Outcome { get; init; } = AuditOutcome.Success;

    public string? ActorId { get; init; }
    public string? ActorName { get; init; }
    public AuditActorType ActorType { get; init; } = AuditActorType.User;

    public string? TargetId { get; init; }
    public string? TargetType { get; init; }
    public string? TargetName { get; init; }

    public string? SourceComponent { get; init; }
    public string? CorrelationId { get; init; }

    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>Creates a builder for a user-initiated audit event.</summary>
    public static AuditEventBuilder UserAction(string eventName, AuditAction action)
        => new(eventName, AuditCategory.User, action, AuditActorType.User);

    /// <summary>Creates a builder for a server/game audit event.</summary>
    public static AuditEventBuilder ServerAction(string eventName, AuditAction action)
        => new(eventName, AuditCategory.Server, action, AuditActorType.System);

    /// <summary>Creates a builder for a system/background audit event.</summary>
    public static AuditEventBuilder SystemAction(string eventName, AuditAction action)
        => new(eventName, AuditCategory.System, action, AuditActorType.Service);
}
