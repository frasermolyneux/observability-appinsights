# MX.Observability.ApplicationInsights

Hosting-agnostic core for the MX Observability libraries: configurable telemetry filter implementation, structured audit logging and scheduled job telemetry for .NET 9/10 applications using Azure Application Insights.

**You normally do not reference this package directly.** Reference one of the host-specific adapter packages instead, which transitively reference this core package and additionally wire the telemetry filter into the correct SDK pipeline:

- [`MX.Observability.ApplicationInsights.AspNetCore`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights.AspNetCore) — for hosts using `AddApplicationInsightsTelemetry()`
- [`MX.Observability.ApplicationInsights.WorkerService`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights.WorkerService) — for hosts using `AddApplicationInsightsTelemetryWorkerService()`

See the [GitHub repository](https://github.com/frasermolyneux/observability-appinsights) for full documentation.

