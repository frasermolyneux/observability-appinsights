# Copilot Instructions

> Shared conventions: see [`.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md`](../../.github-copilot/.github/instructions/dotnet-nuget-library.instructions.md) for general .NET NuGet library standards (multi-targeting, packaging, versioning, CI/CD).

## Org conventions via MCP (when available)

If a `frasermolyneux-copilot` MCP server is configured in your client (`.vscode/mcp.json`, the GitHub Copilot coding-agent MCP config at `.github/copilot/mcp_config.json`, or an equivalent stdio MCP wire-up), **prefer its tools** over your own assumptions when answering questions about org standards, branching, workflows, Terraform, .NET projects, Azure patterns, or shared library / platform consumption contracts. The tool surface is `list_instructions`, `get_instruction`, `search_instructions`, plus the matching `_prompts` and `_agents` equivalents (seven tools total). The catalog source-of-truth lives in `frasermolyneux/.github-copilot` — see `mcp-server/README.md` there for the tool contract.

This is **complementary** to the file-load model: if `./.github-copilot/` is checked out in the runner (per `copilot-setup-steps.yml`), continue to read those files directly. If both are available, prefer MCP for freshness. If no MCP server is configured in your client, treat this section as a no-op and fall back to the file paths above.

- **Purpose & layout**: .NET 9/10 library under `src/` providing Application Insights telemetry filtering, structured audit logging, and job telemetry (`MX.Observability.ApplicationInsights`), with tests in `MX.Observability.ApplicationInsights.Tests`. Solution entry is `src/MX.Observability.ApplicationInsights.sln`.
- **Architecture**: Single `TelemetryFilterProcessor` dispatches by telemetry type (dependency/request/trace) using `IOptionsMonitor<TelemetryFilterOptions>` with cached `ParsedFilterRules`. `IAuditLogger` emits structured `EventTelemetry` with `Audit:` prefix. `IJobTelemetry` wraps job lifecycle with audit integration. All registered via `AddObservability()` extension method.
- **Configuration**: Filtering configured via `ApplicationInsights:TelemetryFilter` config section bound to `TelemetryFilterOptions`. Supports live reload via `IOptionsMonitor`. Defaults are sensible for zero-config operation.
- **Testing notes**: Uses xUnit + Moq for `TelemetryClient` verification. Test naming follows `MethodName_Condition_ExpectedResult` pattern.
