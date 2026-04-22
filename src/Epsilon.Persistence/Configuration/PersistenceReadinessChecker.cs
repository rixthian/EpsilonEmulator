using Microsoft.Extensions.Options;

namespace Epsilon.Persistence;

public sealed class PersistenceReadinessChecker : IPersistenceReadinessChecker
{
    private readonly PersistenceOptions _options;

    public PersistenceReadinessChecker(IOptions<PersistenceOptions> options)
    {
        _options = options.Value;
    }

    public PersistenceReadinessReport Check()
    {
        List<string> issues = [];
        string provider = string.IsNullOrWhiteSpace(_options.Provider) ? "InMemory" : _options.Provider;
        bool postgresConfigured = !string.IsNullOrWhiteSpace(_options.PostgresConnectionString);
        bool redisConfigured = !string.IsNullOrWhiteSpace(_options.RedisConnectionString);

        if (!provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) && !provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Persistence provider must be either InMemory or Postgres.");
        }

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) && !postgresConfigured)
        {
            issues.Add("PostgreSQL connection string is missing.");
        }

        return new PersistenceReadinessReport(
            Provider: provider,
            IsReady: issues.Count == 0,
            PostgresConfigured: postgresConfigured,
            RedisConfigured: redisConfigured,
            Issues: issues);
    }
}
