# observability-appinsights

Multi-target .NET observability libraries for Application Insights telemetry filtering, structured auditing, and job lifecycle telemetry. The repository publishes a core package and ASP.NET Core and Worker Service adapters.

## Locations

- Solution: `src/MX.Observability.ApplicationInsights.slnx`
- Core package: `src/MX.Observability.ApplicationInsights`
- Host adapters: `src/MX.Observability.ApplicationInsights.AspNetCore`, `src/MX.Observability.ApplicationInsights.WorkerService`
- Tests: `src/MX.Observability.ApplicationInsights.Tests`
- Usage and configuration: `README.md` and `docs/`

## Commands

```pwsh
dotnet build src/MX.Observability.ApplicationInsights.slnx
dotnet test src/MX.Observability.ApplicationInsights.slnx
dotnet test src/MX.Observability.ApplicationInsights.slnx --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
dotnet format src/MX.Observability.ApplicationInsights.slnx --verify-no-changes
```

## Constraints

- Keep telemetry filtering, audit, and job behavior in the core package; adapters should contain host-specific registration only.
- Preserve configuration keys, default filtering behavior, structured audit fields, and public extension methods.
- Keep the ASP.NET Core and Worker Service adapter types separate because their SDK registration interfaces differ.
- Keep package identities, target frameworks, package READMEs, and `version.json` behavior unchanged unless explicitly requested.
- Build generates packages; do not publish them during validation.

## Documentation

- [Package overview and configuration](README.md)
