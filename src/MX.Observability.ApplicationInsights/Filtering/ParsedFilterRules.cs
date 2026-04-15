using Microsoft.ApplicationInsights.DataContracts;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Filtering;

/// <summary>
/// Immutable, pre-parsed snapshot of filter rules built from <see cref="TelemetryFilterOptions"/>.
/// Rebuilt only when configuration changes. Enables fast per-item filtering via HashSet lookups.
/// </summary>
internal sealed class ParsedFilterRules
{
    // Global
    public bool Enabled { get; }

    // Dependencies
    public bool DependenciesEnabled { get; }
    public double DependencyDurationThresholdMs { get; }
    public bool DependencyFilterAllTypes { get; }
    public HashSet<string> DependencyExcludedTypes { get; }
    public string[] DependencyExcludedTypePrefixes { get; }
    public HashSet<string> DependencyIgnoredTargets { get; }
    public HashSet<string> DependencyRetainedResultCodes { get; }

    // Requests
    public bool RequestsEnabled { get; }
    public double RequestDurationThresholdMs { get; }
    public bool RequestSuccessOnly { get; }
    public string[] RequestExcludedPaths { get; }
    public HashSet<string> RequestExcludedHttpMethods { get; }
    public HashSet<string> RequestRetainedStatusCodes { get; }
    public (int Min, int Max)[] RequestRetainedStatusCodeRanges { get; }

    // Traces
    public bool TracesEnabled { get; }
    public SeverityLevel TraceMinSeverity { get; }
    public HashSet<string> TraceAlwaysRetainCategories { get; }
    public HashSet<string> TraceExcludedCategories { get; }
    public string[] TraceExcludedMessageContains { get; }

    private ParsedFilterRules(TelemetryFilterOptions options)
    {
        Enabled = options.Enabled;

        // Dependencies
        var deps = options.Dependencies;
        DependenciesEnabled = deps.Enabled;
        DependencyDurationThresholdMs = deps.DurationThresholdMs;
        DependencyFilterAllTypes = deps.FilterAllTypes;
        DependencyExcludedTypes = ParseCsvToHashSet(deps.ExcludedTypes);
        DependencyExcludedTypePrefixes = ParseCsvToArray(deps.ExcludedTypePrefixes);
        DependencyIgnoredTargets = ParseCsvToHashSet(deps.IgnoredTargets);
        DependencyRetainedResultCodes = ParseCsvToHashSet(deps.RetainedResultCodes);

        // Requests
        var reqs = options.Requests;
        RequestsEnabled = reqs.Enabled;
        RequestDurationThresholdMs = reqs.DurationThresholdMs;
        RequestSuccessOnly = reqs.SuccessOnly;
        RequestExcludedPaths = ParseCsvToArray(reqs.ExcludedPaths);
        RequestExcludedHttpMethods = ParseCsvToHashSet(reqs.ExcludedHttpMethods);
        RequestRetainedStatusCodes = ParseCsvToHashSet(reqs.RetainedStatusCodes);
        RequestRetainedStatusCodeRanges = ParseStatusCodeRanges(reqs.RetainedStatusCodeRanges);

        // Traces
        var traces = options.Traces;
        TracesEnabled = traces.Enabled;
        TraceMinSeverity = ParseSeverity(traces.MinSeverity);
        TraceAlwaysRetainCategories = ParseCsvToHashSet(traces.AlwaysRetainCategories);
        TraceExcludedCategories = ParseCsvToHashSet(traces.ExcludedCategories);
        TraceExcludedMessageContains = ParseCsvToArray(traces.ExcludedMessageContains);
    }

    public static ParsedFilterRules From(TelemetryFilterOptions options) => new(options);

    private static HashSet<string> ParseCsvToHashSet(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string[] ParseCsvToArray(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static SeverityLevel ParseSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return SeverityLevel.Warning;

        return severity.Trim().ToLowerInvariant() switch
        {
            "verbose" => SeverityLevel.Verbose,
            "information" => SeverityLevel.Information,
            "warning" => SeverityLevel.Warning,
            "error" => SeverityLevel.Error,
            "critical" => SeverityLevel.Critical,
            _ => SeverityLevel.Warning
        };
    }

    private static (int Min, int Max)[] ParseStatusCodeRanges(string? ranges)
    {
        if (string.IsNullOrWhiteSpace(ranges))
            return Array.Empty<(int, int)>();

        var result = new List<(int Min, int Max)>();
        foreach (var part in ranges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dashIndex = part.IndexOf('-');
            if (dashIndex > 0 &&
                int.TryParse(part.AsSpan(0, dashIndex), out var min) &&
                int.TryParse(part.AsSpan(dashIndex + 1), out var max))
            {
                result.Add((min, max));
            }
        }
        return result.ToArray();
    }
}
