namespace MX.Observability.ApplicationInsights.Filtering.Configuration;

public class CustomEventFilterOptions
{
    public bool Enabled { get; set; } = false;
    public string AllowedNames { get; set; } = "";
    public string AllowedNamePrefixes { get; set; } = "";
}