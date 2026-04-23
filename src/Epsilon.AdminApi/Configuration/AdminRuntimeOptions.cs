namespace Epsilon.AdminApi;

public sealed class AdminRuntimeOptions
{
    public const string SectionName = "Admin";

    public string ServiceName { get; set; } = "Epsilon Admin API";

    /// <summary>
    /// Static API key required on all non-health requests via
    /// X-Epsilon-Admin-Key header. Leave empty to disable authentication
    /// (not recommended outside local development).
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional gateway base URL used by the admin surface to query gateway diagnostics.
    /// </summary>
    public string GatewayBaseUrl { get; set; } = "http://127.0.0.1:5000";

    /// <summary>
    /// Optional launcher base URL used by the admin surface to query launcher health.
    /// </summary>
    public string LauncherBaseUrl { get; set; } = "http://127.0.0.1:5001";
}
