# MX Observability - Application Insights

[![Build and Test](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/build-and-test.yml)
[![Code Quality](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/codequality.yml)
[![Release - Version and Tag](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/release-version-and-tag.yml/badge.svg)](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/release-version-and-tag.yml)
[![Release - Publish NuGet](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/release-publish-nuget.yml/badge.svg)](https://github.com/frasermolyneux/observability-appinsights/actions/workflows/release-publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/MX.Observability.ApplicationInsights.svg)](https://www.nuget.org/packages/MX.Observability.ApplicationInsights/)

## Overview

Shared observability library for .NET 9/10 applications using Azure Application Insights. Published as **three** NuGet packages:

| Package | Use when your host calls... |
|---------|------------------------------|
| [`MX.Observability.ApplicationInsights`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights) | (core; referenced transitively) |
| [`MX.Observability.ApplicationInsights.AspNetCore`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights.AspNetCore) | `AddApplicationInsightsTelemetry()` |
| [`MX.Observability.ApplicationInsights.WorkerService`](https://www.nuget.org/packages/MX.Observability.ApplicationInsights.WorkerService) | `AddApplicationInsightsTelemetryWorkerService()` |

Provides three pillars:

1. **Telemetry Filtering** — Configurable `ITelemetryProcessor` that reduces telemetry volume by filtering successful/fast dependencies, requests, and low-severity traces while always retaining failures, slow calls, and errors.
2. **Audit Logging** — Structured `IAuditLogger` with category-specific builders (`UserAction`, `ServerAction`, `SystemAction`) for consistent, queryable audit events across all applications.
3. **Job Telemetry** — `IJobTelemetry` for tracking scheduled job lifecycles (start, complete, fail) with duration metrics and audit trail integration.

> **Why two adapter packages?** The Application Insights SDK ships separate `ITelemetryProcessorFactory` interfaces (and matching `AddApplicationInsightsTelemetryProcessor<T>()` extensions) in the AspNetCore and WorkerService SDK packages — they are *different types in different namespaces*, and each SDK only sees its own. A single combined package would either duplicate hosting dependencies for every consumer or risk extension-method ambiguity. Two thin adapters (each ~40 lines) avoid both problems and let the SDK's own, supported registration path do the work.

## Quick Start — ASP.NET Core

```csharp
using MX.Observability.ApplicationInsights.AspNetCore;

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddObservability();
```

## Quick Start — Worker Service / Functions Isolated

```csharp
using MX.Observability.ApplicationInsights.WorkerService;

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddObservability();
```

## Configuration

Telemetry filtering is configured via `ApplicationInsights:TelemetryFilter` configuration keys (Azure App Configuration, appsettings.json, etc.):

| Key | Default | Description |
|-----|---------|-------------|
| `...:Enabled` | `true` | Global kill-switch |
| `...:Dependencies:Enabled` | `true` | Filter dependencies |
| `...:Dependencies:DurationThresholdMs` | `1000` | Keep slow dependencies |
| `...:Dependencies:FilterAllTypes` | `true` | Filter all types (vs allowlist) |
| `...:Requests:Enabled` | `true` | Filter requests |
| `...:Requests:DurationThresholdMs` | `1000` | Keep slow requests |
| `...:Requests:ExcludedPaths` | `/healthz,/health,/api/health` | Always filter these paths |
| `...:Requests:RetainedStatusCodeRanges` | `400-599` | Always keep errors |
| `...:Traces:Enabled` | `true` | Filter traces |
| `...:Traces:MinSeverity` | `Warning` | Minimum severity to retain |

See full configuration reference in [docs/configuration.md](docs/configuration.md).

## Audit Logging

```csharp
// User-initiated action
auditLogger.LogAudit(AuditEvent.UserAction("AdminActionCreated", AuditAction.Create)
    .WithActor(userId, username)
    .WithTarget(playerId, "Player")
    .WithSource("AdminActions")
    .Build());

// Server/game event
auditLogger.LogAudit(AuditEvent.ServerAction("PlayerConnected", AuditAction.Connect)
    .WithGameContext(gameType, serverId)
    .WithPlayer(playerGuid, username)
    .Build());

// System/background job
auditLogger.LogAudit(AuditEvent.SystemAction("BanImported", AuditAction.Import)
    .WithService("BanFileProcessor")
    .WithTarget(playerGuid, "Player")
    .Build());
```

## Job Telemetry

```csharp
// One-liner wrapper
await jobTelemetry.ExecuteAsync("MapImageSync", async () => await DoWork());

// Explicit control
await using var job = jobTelemetry.StartJob("DataSync");
var count = await ProcessRecords();
job.Complete(new() { ["RecordsProcessed"] = count.ToString() });
```

## Contributing

Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## Security

Please read the [security](SECURITY.md) guidance.
