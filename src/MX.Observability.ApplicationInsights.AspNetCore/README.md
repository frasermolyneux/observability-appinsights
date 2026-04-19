# MX.Observability.ApplicationInsights.AspNetCore

ASP.NET Core adapter for [`MX.Observability.ApplicationInsights`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights).

Wires the configurable telemetry filter into the Application Insights pipeline using the ASP.NET Core SDK's `AddApplicationInsightsTelemetryProcessor<T>()` extension method.

## Usage

```csharp
using MX.Observability.ApplicationInsights.AspNetCore;

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddObservability();
```

Use this package when your host calls `AddApplicationInsightsTelemetry()` (ASP.NET Core web/API apps and Worker apps that opt in to the ASP.NET Core SDK).

For Worker Service / Azure Functions (isolated) hosts that call `AddApplicationInsightsTelemetryWorkerService()`, use `MX.Observability.ApplicationInsights.WorkerService` instead.

See the [core package README](https://www.nuget.org/packages/MX.Observability.ApplicationInsights) for filter configuration, audit logging and job telemetry usage.
