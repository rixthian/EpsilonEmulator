namespace Epsilon.AdminApi;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public string PostgresConnectionString { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
}

