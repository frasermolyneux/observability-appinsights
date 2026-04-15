namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class RequestFilterOptions
{
    public bool Enabled { get; set; } = true;
    public double DurationThresholdMs { get; set; } = 1000;
    public bool SuccessOnly { get; set; } = true;
    public string ExcludedPaths { get; set; } = "/healthz,/health,/api/health";
    public string ExcludedHttpMethods { get; set; } = "";
    public string RetainedStatusCodes { get; set; } = "";
    public string RetainedStatusCodeRanges { get; set; } = "400-599";
}
