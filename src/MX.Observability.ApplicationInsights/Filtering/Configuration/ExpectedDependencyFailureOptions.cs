namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class ExpectedDependencyFailureOptions
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string NamePrefix { get; set; } = "";
    public string NameContains { get; set; } = "";
    public string NameSuffix { get; set; } = "";
    public string Target { get; set; } = "";
    public string TargetPrefix { get; set; } = "";
    public string TargetContains { get; set; } = "";
    public string TargetSuffix { get; set; } = "";
    public string ResultCode { get; set; } = "";
    public bool MatchEmptyResultCode { get; set; }
}
