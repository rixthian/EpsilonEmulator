namespace Epsilon.AdminApi;

public sealed class AdminRuntimeOptions
{
    public const string SectionName = "Admin";

    public string ServiceName { get; set; } = "Epsilon Admin API";

    /// <summary>
    /// Static API key required on all non-health requests via
    /// X-Epsilon-Admin-Key header.
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Allows missing admin API keys only for explicit local development flows.
    /// Keep false for shared machines, tunnels, LAN access, staging, and production.
    /// </summary>
    public bool AllowMissingAdminApiKeyForLocalDevelopment { get; set; }

    /// <summary>
    /// Optional gateway base URL used by the admin surface to query gateway diagnostics.
    /// </summary>
    public string GatewayBaseUrl { get; set; } = "http://127.0.0.1:5100";

    /// <summary>
    /// Optional launcher base URL used by the admin surface to query launcher health.
    /// </summary>
    public string LauncherBaseUrl { get; set; } = "http://127.0.0.1:5001";
}
