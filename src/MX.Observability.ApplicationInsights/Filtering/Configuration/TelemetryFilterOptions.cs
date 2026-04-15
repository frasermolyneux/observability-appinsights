namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class TelemetryFilterOptions
{
    public const string SectionName = "ApplicationInsights:TelemetryFilter";

    public bool Enabled { get; set; } = true;
    public DependencyFilterOptions Dependencies { get; set; } = new();
    public RequestFilterOptions Requests { get; set; } = new();
    public TraceFilterOptions Traces { get; set; } = new();
}
