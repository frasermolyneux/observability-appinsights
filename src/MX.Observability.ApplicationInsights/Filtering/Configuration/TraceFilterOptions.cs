namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class TraceFilterOptions
{
    public bool Enabled { get; set; } = true;
    public string MinSeverity { get; set; } = "Warning";
    public string AlwaysRetainCategories { get; set; } = "";
    public string ExcludedCategories { get; set; } = "";
    public string ExcludedMessageContains { get; set; } = "";
}
