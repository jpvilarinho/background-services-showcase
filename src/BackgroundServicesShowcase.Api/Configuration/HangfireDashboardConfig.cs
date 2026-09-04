namespace BackgroundServicesShowcase.Api.Configuration;

public sealed class HangfireDashboardConfig
{
    public const string SectionName = "HangfireDashboard";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "changeme-local-only";
}
