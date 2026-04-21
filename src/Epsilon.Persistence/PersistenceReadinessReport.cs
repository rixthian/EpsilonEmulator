namespace Epsilon.Persistence;

public sealed record PersistenceReadinessReport(
    string Provider,
    bool IsReady,
    bool PostgresConfigured,
    bool RedisConfigured,
    List<string> Issues);
