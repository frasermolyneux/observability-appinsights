using MX.Observability.ApplicationInsights.Auditing.Models;

namespace MX.Observability.ApplicationInsights.Tests.Auditing;

[Trait("Category", "Unit")]
public class AuditEventBuilderTests
{
    [Fact]
    public void UserAction_SetsCategory_User()
    {
        var evt = AuditEvent.UserAction("Login", AuditAction.Execute).Build();

        Assert.Equal(AuditCategory.User, evt.Category);
        Assert.Equal(AuditActorType.User, evt.ActorType);
    }

    [Fact]
    public void ServerAction_SetsCategory_Server()
    {
        var evt = AuditEvent.ServerAction("ServerStart", AuditAction.Execute).Build();

        Assert.Equal(AuditCategory.Server, evt.Category);
        Assert.Equal(AuditActorType.System, evt.ActorType);
    }

    [Fact]
    public void SystemAction_SetsCategory_System()
    {
        var evt = AuditEvent.SystemAction("JobRun", AuditAction.Execute).Build();

        Assert.Equal(AuditCategory.System, evt.Category);
        Assert.Equal(AuditActorType.Service, evt.ActorType);
    }

    [Fact]
    public void WithActor_SetsActorIdAndName()
    {
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithActor("user-1", "Alice")
            .Build();

        Assert.Equal("user-1", evt.ActorId);
        Assert.Equal("Alice", evt.ActorName);
    }

    [Fact]
    public void WithService_SetsActorIdAndServiceType()
    {
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithService("my-service")
            .Build();

        Assert.Equal("my-service", evt.ActorId);
        Assert.Equal(AuditActorType.Service, evt.ActorType);
    }

    [Fact]
    public void WithTarget_SetsTargetFields()
    {
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithTarget("t-1", "Document", "MyDoc")
            .Build();

        Assert.Equal("t-1", evt.TargetId);
        Assert.Equal("Document", evt.TargetType);
        Assert.Equal("MyDoc", evt.TargetName);
    }

    [Fact]
    public void WithGameContext_AddsGameTypeAndServerId()
    {
        var serverId = Guid.NewGuid();
        var evt = AuditEvent.ServerAction("Test", AuditAction.Execute)
            .WithGameContext("TF2", serverId)
            .Build();

        Assert.Equal("TF2", evt.Properties["GameType"]);
        Assert.Equal(serverId.ToString(), evt.Properties["ServerId"]);
    }

    [Fact]
    public void WithPlayer_SetsTargetAsPlayer()
    {
        var evt = AuditEvent.ServerAction("Test", AuditAction.Moderate)
            .WithPlayer("player-guid-123", "PlayerName")
            .Build();

        Assert.Equal("player-guid-123", evt.TargetId);
        Assert.Equal("Player", evt.TargetType);
        Assert.Equal("PlayerName", evt.TargetName);
    }

    [Fact]
    public void WithOutcome_SetsOutcome()
    {
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithOutcome(AuditOutcome.Denied)
            .Build();

        Assert.Equal(AuditOutcome.Denied, evt.Outcome);
    }

    [Fact]
    public void WithProperty_AddsCustomProperty()
    {
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithProperty("Key1", "Value1")
            .Build();

        Assert.Equal("Value1", evt.Properties["Key1"]);
    }

    [Fact]
    public void WithProperties_AddsMultipleProperties()
    {
        var props = new Dictionary<string, string>
        {
            ["A"] = "1",
            ["B"] = "2"
        };
        var evt = AuditEvent.UserAction("Test", AuditAction.Read)
            .WithProperties(props)
            .Build();

        Assert.Equal("1", evt.Properties["A"]);
        Assert.Equal("2", evt.Properties["B"]);
    }

    [Fact]
    public void Build_ReturnsCompleteAuditEvent()
    {
        var evt = AuditEvent.UserAction("FullTest", AuditAction.Create)
            .WithActor("actor-1", "ActorName")
            .WithTarget("target-1", "Resource", "MyResource")
            .WithSource("TestController")
            .WithCorrelation("corr-123")
            .WithOutcome(AuditOutcome.Success)
            .WithProperty("Extra", "Data")
            .Build();

        Assert.Equal("FullTest", evt.EventName);
        Assert.Equal(AuditCategory.User, evt.Category);
        Assert.Equal(AuditAction.Create, evt.Action);
        Assert.Equal(AuditActorType.User, evt.ActorType);
        Assert.Equal(AuditOutcome.Success, evt.Outcome);
        Assert.Equal("actor-1", evt.ActorId);
        Assert.Equal("ActorName", evt.ActorName);
        Assert.Equal("target-1", evt.TargetId);
        Assert.Equal("Resource", evt.TargetType);
        Assert.Equal("MyResource", evt.TargetName);
        Assert.Equal("TestController", evt.SourceComponent);
        Assert.Equal("corr-123", evt.CorrelationId);
        Assert.Equal("Data", evt.Properties["Extra"]);
    }
}
