using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using MX.Observability.ApplicationInsights.Filtering.Configuration;

namespace MX.Observability.ApplicationInsights.Filtering;

/// <summary>
/// Application Insights telemetry processor that filters out successful, fast telemetry
/// to reduce volume. Failed calls, slow calls, and errors are always retained.
/// Configuration is live-reloadable via <see cref="IOptionsMonitor{TelemetryFilterOptions}"/>.
/// </summary>
public sealed class TelemetryFilterProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private volatile ParsedFilterRules _rules;

    public TelemetryFilterProcessor(ITelemetryProcessor next, IOptionsMonitor<TelemetryFilterOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _next = next;
        _rules = ParsedFilterRules.From(optionsMonitor.CurrentValue);
        optionsMonitor.OnChange(opts => _rules = ParsedFilterRules.From(opts));
    }

    public void Process(ITelemetry item)
    {
        var rules = _rules;

        if (!rules.Enabled)
        {
            _next.Process(item);
            return;
        }

        var shouldFilter = item switch
        {
            DependencyTelemetry dep => ShouldFilterDependency(dep, rules),
            RequestTelemetry req => ShouldFilterRequest(req, rules),
            TraceTelemetry trace => ShouldFilterTrace(trace, rules),
            _ => false
        };

        if (!shouldFilter)
            _next.Process(item);
    }

    internal static bool ShouldFilterDependency(DependencyTelemetry dependency, ParsedFilterRules rules)
    {
        if (!rules.DependenciesEnabled)
            return false;

        // Always filter ignored targets (e.g. localhost)
        if (rules.DependencyIgnoredTargets.Count > 0 &&
            !string.IsNullOrEmpty(dependency.Target) &&
            rules.DependencyIgnoredTargets.Contains(dependency.Target))
            return true;

        // Check if this dependency type should be filtered
        if (!rules.DependencyFilterAllTypes)
        {
            if (string.IsNullOrEmpty(dependency.Type))
                return false;

            var typeMatches =
                rules.DependencyExcludedTypes.Contains(dependency.Type) ||
                rules.DependencyExcludedTypePrefixes.Any(p =>
                    dependency.Type.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!typeMatches)
                return false;
        }

        // Always retain result codes of interest (e.g. 429, 503)
        if (rules.DependencyRetainedResultCodes.Count > 0 &&
            !string.IsNullOrEmpty(dependency.ResultCode) &&
            rules.DependencyRetainedResultCodes.Contains(dependency.ResultCode))
            return false;

        // Always retain failed calls
        if (dependency.Success != true)
            return false;

        // Always retain slow calls
        if (dependency.Duration.TotalMilliseconds > rules.DependencyDurationThresholdMs)
            return false;

        return true;
    }

    internal static bool ShouldFilterRequest(RequestTelemetry request, ParsedFilterRules rules)
    {
        if (!rules.RequestsEnabled)
            return false;

        // Always filter excluded paths (health checks)
        if (rules.RequestExcludedPaths.Length > 0 && request.Url is not null)
        {
            var path = request.Url.AbsolutePath;
            if (rules.RequestExcludedPaths.Any(p =>
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // Always filter excluded HTTP methods (OPTIONS, HEAD)
        if (rules.RequestExcludedHttpMethods.Count > 0 &&
            !string.IsNullOrEmpty(request.Name))
        {
            // Request.Name often starts with HTTP method (e.g. "GET /api/health")
            var spaceIndex = request.Name.IndexOf(' ');
            var method = spaceIndex > 0 ? request.Name[..spaceIndex] : request.Name;
            if (rules.RequestExcludedHttpMethods.Contains(method))
                return true;
        }

        // Always retain specific status codes
        if (int.TryParse(request.ResponseCode, out var statusCode))
        {
            if (rules.RequestRetainedStatusCodes.Contains(request.ResponseCode))
                return false;

            foreach (var (min, max) in rules.RequestRetainedStatusCodeRanges)
            {
                if (statusCode >= min && statusCode <= max)
                    return false;
            }
        }

        // If SuccessOnly, only filter successful requests
        if (rules.RequestSuccessOnly && request.Success != true)
            return false;

        // Always retain slow requests
        if (request.Duration.TotalMilliseconds > rules.RequestDurationThresholdMs)
            return false;

        return true;
    }

    internal static bool ShouldFilterTrace(TraceTelemetry trace, ParsedFilterRules rules)
    {
        if (!rules.TracesEnabled)
            return false;

        // Extract category from properties (ILogger sets "CategoryName")
        var category = trace.Properties.TryGetValue("CategoryName", out var cat) ? cat : null;

        // Always filter excluded categories
        if (rules.TraceExcludedCategories.Count > 0 &&
            !string.IsNullOrEmpty(category) &&
            rules.TraceExcludedCategories.Contains(category))
            return true;

        // Always filter messages containing excluded substrings
        if (rules.TraceExcludedMessageContains.Length > 0 &&
            !string.IsNullOrEmpty(trace.Message))
        {
            foreach (var substring in rules.TraceExcludedMessageContains)
            {
                if (trace.Message.Contains(substring, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        // Always retain specific categories
        if (rules.TraceAlwaysRetainCategories.Count > 0 &&
            !string.IsNullOrEmpty(category) &&
            rules.TraceAlwaysRetainCategories.Contains(category))
            return false;

        // Filter by severity
        var severity = trace.SeverityLevel ?? SeverityLevel.Verbose;
        if (severity < rules.TraceMinSeverity)
            return true;

        return false;
    }
}
