using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Auditing.Models;
using MX.Observability.ApplicationInsights.Tests.Helpers;

namespace MX.Observability.ApplicationInsights.Tests.Auditing;

[Trait("Category", "Unit")]
public class AuditLoggerTests
{
    private readonly List<ITelemetry> _sentTelemetry = new();
    private readonly ApplicationInsightsAuditLogger _logger;

    public AuditLoggerTests()
    {
        var channel = new StubTelemetryChannel { OnSend = t => _sentTelemetry.Add(t) };
        var config = new TelemetryConfiguration { TelemetryChannel = channel };
        var client = new TelemetryClient(config);
        _logger = new ApplicationInsightsAuditLogger(client);
    }

    [Fact]
    public void LogAudit_EmitsEventWithAuditPrefix()
    {
        var auditEvent = AuditEvent.UserAction("Login", AuditAction.Execute).Build();

        _logger.LogAudit(auditEvent);

        var evt = Assert.Single(_sentTelemetry);
        var eventTelemetry = Assert.IsType<EventTelemetry>(evt);
        Assert.Equal("Audit:Login", eventTelemetry.Name);
    }

    [Fact]
    public void LogAudit_SetsStandardProperties()
    {
        var auditEvent = AuditEvent.UserAction("Login", AuditAction.Execute).Build();

        _logger.LogAudit(auditEvent);

        var evt = Assert.IsType<EventTelemetry>(Assert.Single(_sentTelemetry));
        Assert.Equal("User", evt.Properties["Audit.Category"]);
        Assert.Equal("Execute", evt.Properties["Audit.Action"]);
        Assert.Equal("Success", evt.Properties["Audit.Outcome"]);
        Assert.Equal("User", evt.Properties["Audit.ActorType"]);
    }

    [Fact]
    public void LogAudit_SetsOptionalProperties_WhenProvided()
    {
        var auditEvent = AuditEvent.UserAction("Update", AuditAction.Update)
            .WithActor("user-123", "John")
            .WithTarget("resource-1", "Document", "MyDoc")
            .WithSource("ApiController")
            .WithCorrelation("corr-abc")
            .Build();

        _logger.LogAudit(auditEvent);

        var evt = Assert.IsType<EventTelemetry>(Assert.Single(_sentTelemetry));
        Assert.Equal("user-123", evt.Properties["Audit.ActorId"]);
        Assert.Equal("John", evt.Properties["Audit.ActorName"]);
        Assert.Equal("resource-1", evt.Properties["Audit.TargetId"]);
        Assert.Equal("Document", evt.Properties["Audit.TargetType"]);
        Assert.Equal("MyDoc", evt.Properties["Audit.TargetName"]);
        Assert.Equal("ApiController", evt.Properties["Audit.SourceComponent"]);
        Assert.Equal("corr-abc", evt.Properties["Audit.CorrelationId"]);
    }

    [Fact]
    public void LogAudit_OmitsOptionalProperties_WhenNull()
    {
        var auditEvent = AuditEvent.UserAction("Login", AuditAction.Execute).Build();

        _logger.LogAudit(auditEvent);

        var evt = Assert.IsType<EventTelemetry>(Assert.Single(_sentTelemetry));
        Assert.False(evt.Properties.ContainsKey("Audit.ActorId"));
        Assert.False(evt.Properties.ContainsKey("Audit.ActorName"));
        Assert.False(evt.Properties.ContainsKey("Audit.TargetId"));
        Assert.False(evt.Properties.ContainsKey("Audit.TargetType"));
        Assert.False(evt.Properties.ContainsKey("Audit.TargetName"));
        Assert.False(evt.Properties.ContainsKey("Audit.SourceComponent"));
        Assert.False(evt.Properties.ContainsKey("Audit.CorrelationId"));
    }

    [Fact]
    public void LogAudit_IncludesCustomProperties()
    {
        var auditEvent = AuditEvent.UserAction("Login", AuditAction.Execute)
            .WithProperty("IpAddress", "192.168.1.1")
            .WithProperty("Browser", "Chrome")
            .Build();

        _logger.LogAudit(auditEvent);

        var evt = Assert.IsType<EventTelemetry>(Assert.Single(_sentTelemetry));
        Assert.Equal("192.168.1.1", evt.Properties["IpAddress"]);
        Assert.Equal("Chrome", evt.Properties["Browser"]);
    }

    [Fact]
    public void LogAudit_NullEvent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _logger.LogAudit(null!));
    }
}
