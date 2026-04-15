using MX.Observability.ApplicationInsights.Auditing.Models;

namespace MX.Observability.ApplicationInsights.Auditing;

/// <summary>
/// Fluent builder for constructing <see cref="AuditEvent"/> instances with category-specific guidance.
/// </summary>
public sealed class AuditEventBuilder
{
    private readonly string _eventName;
    private readonly AuditCategory _category;
    private readonly AuditAction _action;
    private AuditActorType _actorType;
    private AuditOutcome _outcome = AuditOutcome.Success;
    private string? _actorId;
    private string? _actorName;
    private string? _targetId;
    private string? _targetType;
    private string? _targetName;
    private string? _sourceComponent;
    private string? _correlationId;
    private readonly Dictionary<string, string> _properties = new();

    public AuditEventBuilder(string eventName, AuditCategory category, AuditAction action, AuditActorType actorType)
    {
        _eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        _category = category;
        _action = action;
        _actorType = actorType;
    }

    /// <summary>Sets the authenticated user who performed the action.</summary>
    public AuditEventBuilder WithActor(string actorId, string? actorName = null)
    {
        _actorId = actorId;
        _actorName = actorName;
        return this;
    }

    /// <summary>Sets a service or system identity as the actor.</summary>
    public AuditEventBuilder WithService(string serviceName)
    {
        _actorId = serviceName;
        _actorType = AuditActorType.Service;
        return this;
    }

    /// <summary>Sets the entity being acted upon.</summary>
    public AuditEventBuilder WithTarget(string targetId, string targetType, string? targetName = null)
    {
        _targetId = targetId;
        _targetType = targetType;
        _targetName = targetName;
        return this;
    }

    /// <summary>Adds game context properties (GameType, ServerId). Typical for server events.</summary>
    public AuditEventBuilder WithGameContext(string gameType, Guid serverId)
    {
        _properties["GameType"] = gameType;
        _properties["ServerId"] = serverId.ToString();
        return this;
    }

    /// <summary>Adds player identification properties.</summary>
    public AuditEventBuilder WithPlayer(string playerGuid, string? username = null)
    {
        _targetId = playerGuid;
        _targetType = "Player";
        if (username is not null)
            _targetName = username;
        return this;
    }

    /// <summary>Sets the source component (controller, function, service).</summary>
    public AuditEventBuilder WithSource(string sourceComponent)
    {
        _sourceComponent = sourceComponent;
        return this;
    }

    /// <summary>Sets the outcome of the action.</summary>
    public AuditEventBuilder WithOutcome(AuditOutcome outcome)
    {
        _outcome = outcome;
        return this;
    }

    /// <summary>Sets a correlation ID for request tracing.</summary>
    public AuditEventBuilder WithCorrelation(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>Adds a custom property.</summary>
    public AuditEventBuilder WithProperty(string key, string value)
    {
        _properties[key] = value;
        return this;
    }

    /// <summary>Adds multiple custom properties.</summary>
    public AuditEventBuilder WithProperties(Dictionary<string, string> properties)
    {
        foreach (var (key, value) in properties)
            _properties[key] = value;
        return this;
    }

    /// <summary>Builds the <see cref="AuditEvent"/>.</summary>
    public AuditEvent Build()
    {
        return new AuditEvent
        {
            EventName = _eventName,
            Category = _category,
            Action = _action,
            ActorType = _actorType,
            Outcome = _outcome,
            ActorId = _actorId,
            ActorName = _actorName,
            TargetId = _targetId,
            TargetType = _targetType,
            TargetName = _targetName,
            SourceComponent = _sourceComponent,
            CorrelationId = _correlationId,
            Properties = new Dictionary<string, string>(_properties)
        };
    }
}
