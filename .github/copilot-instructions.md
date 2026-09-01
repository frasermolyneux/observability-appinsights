# Copilot Instructions

This repository publishes Application Insights observability packages for telemetry filtering, structured audit events, and job lifecycle telemetry.

## Runtime and layout

- SDK: `10.0.301` from `global.json`; package and test projects target `net9.0` and `net10.0`.
- Solution: `src/MX.Observability.ApplicationInsights.slnx`.
- Core package: `MX.Observability.ApplicationInsights`.
- Adapters: `MX.Observability.ApplicationInsights.AspNetCore` and `MX.Observability.ApplicationInsights.WorkerService`.
- Tests: `MX.Observability.ApplicationInsights.Tests`.

## Repository rules

- Put hosting-agnostic filtering, auditing, and job telemetry behavior in the core package.
- Keep adapter packages thin and host-specific; their Application Insights SDK registration interfaces are distinct and must not be collapsed.
- Preserve `ApplicationInsights:TelemetryFilter` keys, safe zero-configuration defaults, live reload through `IOptionsMonitor`, structured audit properties, and failure-retention behavior.
- Treat `AddObservability()`, filtering options, audit builders, and job telemetry interfaces as public package contracts.
- Package IDs, target frameworks, package READMEs, generated package metadata, and NBGV configuration in `version.json` are release boundaries.
- Never add credentials or publish packages during routine validation.

## Validation

```pwsh
dotnet build src/MX.Observability.ApplicationInsights.slnx
dotnet test src/MX.Observability.ApplicationInsights.slnx
dotnet test src/MX.Observability.ApplicationInsights.slnx --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
dotnet format src/MX.Observability.ApplicationInsights.slnx --verify-no-changes
```

Usage and configuration contracts are documented in `README.md`.
