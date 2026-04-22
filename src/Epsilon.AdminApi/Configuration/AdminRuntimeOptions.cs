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
}
