using Microsoft.Extensions.Options;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresDataSourceProvider : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresDataSourceProvider(IOptions<PersistenceOptions> options)
    {
        string connectionString = options.Value.PostgresConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string is required when Infrastructure.Provider is set to Postgres.");
        }

        NpgsqlDataSourceBuilder builder = new(connectionString);
        _dataSource = builder.Build();
    }

    public ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _dataSource.DisposeAsync();
    }
}
