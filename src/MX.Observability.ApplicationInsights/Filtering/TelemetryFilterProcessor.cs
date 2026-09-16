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
            EventTelemetry evt => ShouldFilterCustomEvent(evt, rules),
            _ => false
        };

        if (!shouldFilter)
        {
            _next.Process(item);
        }
    }

    internal static bool ShouldFilterDependency(DependencyTelemetry dependency, ParsedFilterRules rules)
    {
        if (!rules.DependenciesEnabled)
        {
            return false;
        }

        // Always filter ignored targets (e.g. localhost)
        if (rules.DependencyIgnoredTargets.Count > 0 &&
            !string.IsNullOrEmpty(dependency.Target) &&
            rules.DependencyIgnoredTargets.Contains(dependency.Target))
        {
            return true;
        }

        // Check if this dependency type should be filtered
        if (!rules.DependencyFilterAllTypes)
        {
            if (string.IsNullOrEmpty(dependency.Type))
            {
                return false;
            }

            var typeMatches =
                rules.DependencyExcludedTypes.Contains(dependency.Type) ||
                rules.DependencyExcludedTypePrefixes.Any(p =>
                    dependency.Type.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!typeMatches)
            {
                return false;
            }
        }

        // Always retain result codes of interest (e.g. 429, 503)
        if (rules.DependencyRetainedResultCodes.Count > 0 &&
            !string.IsNullOrEmpty(dependency.ResultCode) &&
            rules.DependencyRetainedResultCodes.Contains(dependency.ResultCode))
        {
            return false;
        }

        if (dependency.Success != true)
        {
            if (dependency.Success == false &&
                rules.DependencyExpectedFailures.Any(rule => rule.Matches(dependency)))
            {
                // Expected-failure rules can never suppress throttling, server failures,
                // timeouts, cancellations, or slow calls.
                return !IsCriticalDependencyFailure(dependency) &&
                    dependency.Duration.TotalMilliseconds <= rules.DependencyDurationThresholdMs;
            }

            return false;
        }

        // Always retain slow calls
        return dependency.Duration.TotalMilliseconds <= rules.DependencyDurationThresholdMs;
    }

    private static bool IsCriticalDependencyFailure(DependencyTelemetry dependency)
    {
        if (TryParseStatusCode(dependency.ResultCode, out var statusCode) &&
            (statusCode is 408 or 429 or 499 or 503 || statusCode >= 500))
        {
            return true;
        }

        return ContainsTimeoutOrCancellationMarker(dependency.ResultCode) ||
            dependency.Properties.Any(property =>
                ContainsTimeoutOrCancellationMarker(property.Key) ||
                ContainsTimeoutOrCancellationMarker(property.Value));
    }

    private static bool TryParseStatusCode(string? resultCode, out int statusCode)
    {
        statusCode = 0;
        if (string.IsNullOrWhiteSpace(resultCode))
        {
            return false;
        }

        var trimmed = resultCode.Trim();
        if (int.TryParse(trimmed, out statusCode))
        {
            return true;
        }

        return trimmed.Length >= 3 &&
            int.TryParse(trimmed.AsSpan(0, 3), out statusCode);
    }

    private static bool ContainsTimeoutOrCancellationMarker(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            (value.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
             value.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
             value.Contains("cancelled", StringComparison.OrdinalIgnoreCase) ||
             value.Contains("canceled", StringComparison.OrdinalIgnoreCase));
    }

    internal static bool ShouldFilterRequest(RequestTelemetry request, ParsedFilterRules rules)
    {
        if (!rules.RequestsEnabled)
        {
            return false;
        }

        // Always filter excluded paths (health checks)
        if (rules.RequestExcludedPaths.Length > 0 && request.Url is not null)
        {
            var path = request.Url.AbsolutePath;
            if (rules.RequestExcludedPaths.Any(p =>
                path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(p, StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{p}/", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // Always filter excluded HTTP methods (OPTIONS, HEAD)
        if (rules.RequestExcludedHttpMethods.Count > 0 &&
            !string.IsNullOrEmpty(request.Name))
        {
            // Request.Name often starts with HTTP method (e.g. "GET /api/health/live")
            var spaceIndex = request.Name.IndexOf(' ');
            var method = spaceIndex > 0 ? request.Name[..spaceIndex] : request.Name;
            if (rules.RequestExcludedHttpMethods.Contains(method))
            {
                return true;
            }
        }

        // Always retain specific status codes
        if (int.TryParse(request.ResponseCode, out var statusCode))
        {
            if (rules.RequestRetainedStatusCodes.Contains(request.ResponseCode))
            {
                return false;
            }

            foreach (var (min, max) in rules.RequestRetainedStatusCodeRanges)
            {
                if (statusCode >= min && statusCode <= max)
                {
                    return false;
                }
            }
        }

        // If SuccessOnly, only filter successful requests
        if (rules.RequestSuccessOnly && request.Success != true)
        {
            return false;
        }

        // Always retain slow requests
        if (request.Duration.TotalMilliseconds > rules.RequestDurationThresholdMs)
        {
            return false;
        }

        return true;
    }

    internal static bool ShouldFilterTrace(TraceTelemetry trace, ParsedFilterRules rules)
    {
        if (!rules.TracesEnabled)
        {
            return false;
        }

        // Extract category from properties (ILogger sets "CategoryName")
        var category = trace.Properties.TryGetValue("CategoryName", out var cat) ? cat : null;

        // Always filter excluded categories
        if (rules.TraceExcludedCategories.Count > 0 &&
            !string.IsNullOrEmpty(category) &&
            rules.TraceExcludedCategories.Contains(category))
        {
            return true;
        }

        // Always filter messages containing excluded substrings
        if (rules.TraceExcludedMessageContains.Length > 0 &&
            !string.IsNullOrEmpty(trace.Message))
        {
            foreach (var substring in rules.TraceExcludedMessageContains)
            {
                if (trace.Message.Contains(substring, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // Always retain specific categories
        if (rules.TraceAlwaysRetainCategories.Count > 0 &&
            !string.IsNullOrEmpty(category) &&
            rules.TraceAlwaysRetainCategories.Contains(category))
        {
            return false;
        }

        // Filter by severity
        var severity = trace.SeverityLevel ?? SeverityLevel.Verbose;
        if (severity < rules.TraceMinSeverity)
        {
            return true;
        }

        return false;
    }

    internal static bool ShouldFilterCustomEvent(EventTelemetry customEvent, ParsedFilterRules rules)
    {
        if (!rules.CustomEventsEnabled)
        {
            return false;
        }

        // Fail open if no allow-list is configured.
        if (rules.CustomEventAllowedNames.Count == 0 && rules.CustomEventAllowedNamePrefixes.Length == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(customEvent.Name))
        {
            return true;
        }

        if (rules.CustomEventAllowedNames.Contains(customEvent.Name))
        {
            return false;
        }

        foreach (var prefix in rules.CustomEventAllowedNamePrefixes)
        {
            if (customEvent.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
