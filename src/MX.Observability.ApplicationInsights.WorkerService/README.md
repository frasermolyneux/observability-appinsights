# MX.Observability.ApplicationInsights.WorkerService

Worker Service / Azure Functions (isolated) adapter for [`MX.Observability.ApplicationInsights`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights).

Wires the configurable telemetry filter into the Application Insights pipeline using the Worker Service SDK's `AddApplicationInsightsTelemetryProcessor<T>()` extension method.

## Usage

```csharp
using MX.Observability.ApplicationInsights.WorkerService;

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddObservability();
```

Use this package when your host calls `AddApplicationInsightsTelemetryWorkerService()` (Worker Services, console apps, Azure Functions isolated worker).

For ASP.NET Core hosts that call `AddApplicationInsightsTelemetry()`, use `MX.Observability.ApplicationInsights.AspNetCore` instead.

See the [core package README](https://www.nuget.org/packages/MX.Observability.ApplicationInsights) for filter configuration, audit logging and job telemetry usage.
