namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class DependencyFilterOptions
{
    public bool Enabled { get; set; } = true;
    public double DurationThresholdMs { get; set; } = 1000;
    public bool FilterAllTypes { get; set; } = true;
    public string ExcludedTypes { get; set; } = "";
    public string ExcludedTypePrefixes { get; set; } = "";
    public string IgnoredTargets { get; set; } = "";
    public string RetainedResultCodes { get; set; } = "";
}
