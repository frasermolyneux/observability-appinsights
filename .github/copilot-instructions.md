# Copilot Instructions

> Shared conventions: see [`.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md`](../../.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md) for general .NET NuGet library standards (multi-targeting, packaging, versioning, CI/CD).

- **Purpose & layout**: .NET 9/10 library under `src/` providing Application Insights telemetry filtering, structured audit logging, and job telemetry (`MX.Observability.ApplicationInsights`), with tests in `MX.Observability.ApplicationInsights.Tests`. Solution entry is `src/MX.Observability.ApplicationInsights.sln`.
- **Architecture**: Single `TelemetryFilterProcessor` dispatches by telemetry type (dependency/request/trace) using `IOptionsMonitor<TelemetryFilterOptions>` with cached `ParsedFilterRules`. `IAuditLogger` emits structured `EventTelemetry` with `Audit:` prefix. `IJobTelemetry` wraps job lifecycle with audit integration. All registered via `AddObservability()` extension method.
- **Configuration**: Filtering configured via `ApplicationInsights:TelemetryFilter` config section bound to `TelemetryFilterOptions`. Supports live reload via `IOptionsMonitor`. Defaults are sensible for zero-config operation.
- **Testing notes**: Uses xUnit + Moq for `TelemetryClient` verification. Test naming follows `MethodName_Condition_ExpectedResult` pattern.
