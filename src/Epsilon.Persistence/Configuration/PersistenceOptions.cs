namespace Epsilon.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Infrastructure";

    public string Provider { get; set; } = "InMemory";
    public string MongoConnectionString { get; set; } = string.Empty;
    public string MongoDatabaseName { get; set; } = "epsilon_emulator";
    public string PostgresConnectionString { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
}
