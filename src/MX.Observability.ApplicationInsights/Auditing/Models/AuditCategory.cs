namespace MX.Observability.ApplicationInsights.Auditing.Models;

/// <summary>
/// Categorises the source of an audit event.
/// </summary>
public enum AuditCategory
{
    /// <summary>Action initiated by an authenticated user.</summary>
    User,
    /// <summary>Server or game-related event (system-initiated, game context).</summary>
    Server,
    /// <summary>System-to-system or background job action.</summary>
    System
}
