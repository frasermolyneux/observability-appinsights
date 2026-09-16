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
    public ExpectedDependencyFailureRule[] DependencyExpectedFailures { get; }

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

    // Custom events
    public bool CustomEventsEnabled { get; }
    public HashSet<string> CustomEventAllowedNames { get; }
    public string[] CustomEventAllowedNamePrefixes { get; }

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
        DependencyExpectedFailures = ParseExpectedDependencyFailures(deps.ExpectedFailures);

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

        // Custom events
        var customEvents = options.CustomEvents;
        CustomEventsEnabled = customEvents.Enabled;
        CustomEventAllowedNames = ParseCsvToHashSet(customEvents.AllowedNames);
        CustomEventAllowedNamePrefixes = ParseCsvToArray(customEvents.AllowedNamePrefixes);
    }

    public static ParsedFilterRules From(TelemetryFilterOptions options) => new(options);

    private static HashSet<string> ParseCsvToHashSet(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return new HashSet<string>(
            csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string[] ParseCsvToArray(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static SeverityLevel ParseSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return SeverityLevel.Warning;
        }

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
        {
            return Array.Empty<(int, int)>();
        }

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

    private static ExpectedDependencyFailureRule[] ParseExpectedDependencyFailures(
        IEnumerable<ExpectedDependencyFailureOptions>? expectedFailures)
    {
        if (expectedFailures is null)
        {
            return [];
        }

        return expectedFailures
            .Select(ExpectedDependencyFailureRule.TryCreate)
            .Where(rule => rule is not null)
            .Cast<ExpectedDependencyFailureRule>()
            .ToArray();
    }
}

internal sealed class ExpectedDependencyFailureRule
{
    private ExpectedDependencyFailureRule(ExpectedDependencyFailureOptions options)
    {
        Type = Normalize(options.Type);
        Name = Normalize(options.Name);
        NamePrefix = Normalize(options.NamePrefix);
        NameContains = Normalize(options.NameContains);
        NameSuffix = Normalize(options.NameSuffix);
        Target = Normalize(options.Target);
        TargetPrefix = Normalize(options.TargetPrefix);
        TargetContains = Normalize(options.TargetContains);
        TargetSuffix = Normalize(options.TargetSuffix);
        ResultCode = Normalize(options.ResultCode);
        MatchEmptyResultCode = options.MatchEmptyResultCode;
    }

    private string Type { get; }
    private string Name { get; }
    private string NamePrefix { get; }
    private string NameContains { get; }
    private string NameSuffix { get; }
    private string Target { get; }
    private string TargetPrefix { get; }
    private string TargetContains { get; }
    private string TargetSuffix { get; }
    private string ResultCode { get; }
    private bool MatchEmptyResultCode { get; }

    public static ExpectedDependencyFailureRule? TryCreate(ExpectedDependencyFailureOptions options)
    {
        var hasIdentityConstraint =
            !string.IsNullOrWhiteSpace(options.Name) ||
            !string.IsNullOrWhiteSpace(options.NamePrefix) ||
            !string.IsNullOrWhiteSpace(options.NameContains) ||
            !string.IsNullOrWhiteSpace(options.NameSuffix) ||
            !string.IsNullOrWhiteSpace(options.Target) ||
            !string.IsNullOrWhiteSpace(options.TargetPrefix) ||
            !string.IsNullOrWhiteSpace(options.TargetContains) ||
            !string.IsNullOrWhiteSpace(options.TargetSuffix);
        var hasResultCode = !string.IsNullOrWhiteSpace(options.ResultCode);

        if (string.IsNullOrWhiteSpace(options.Type) ||
            !hasIdentityConstraint ||
            hasResultCode == options.MatchEmptyResultCode)
        {
            return null;
        }

        return new ExpectedDependencyFailureRule(options);
    }

    public bool Matches(DependencyTelemetry dependency)
    {
        return Equals(dependency.Type, Type) &&
            MatchesText(dependency.Name, Name, NamePrefix, NameContains, NameSuffix) &&
            MatchesText(dependency.Target, Target, TargetPrefix, TargetContains, TargetSuffix) &&
            (MatchEmptyResultCode
                ? string.IsNullOrWhiteSpace(dependency.ResultCode)
                : Equals(dependency.ResultCode, ResultCode));
    }

    private static bool MatchesText(
        string? value,
        string exact,
        string prefix,
        string contains,
        string suffix)
    {
        if (!string.IsNullOrEmpty(exact) && !Equals(value, exact))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(prefix) &&
            (value is null || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(contains) &&
            (value is null || !value.Contains(contains, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(suffix) &&
            (value is null || !value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    private static bool Equals(string? left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value) => value?.Trim() ?? "";
}
