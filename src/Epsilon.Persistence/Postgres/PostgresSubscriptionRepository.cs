using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresSubscriptionRepository : ISubscriptionRepository
{
    private const string Sql = """
        SELECT
            character_id,
            subscription_type,
            activated_at_utc,
            expires_at_utc
        FROM character_subscriptions
        WHERE character_id = @character_id
        ORDER BY subscription_type;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresSubscriptionRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<IReadOnlyList<SubscriptionRecord>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        List<SubscriptionRecord> subscriptions = [];

        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("character_id", characterId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            subscriptions.Add(new SubscriptionRecord(
                CharacterId: new CharacterId(reader.GetInt64(0)),
                SubscriptionType: PostgresModelValueMapper.ParseSubscriptionType(reader.GetString(1)),
                ActivatedAtUtc: reader.GetFieldValue<DateTime>(2),
                ExpiresAtUtc: reader.GetFieldValue<DateTime>(3)));
        }

        return subscriptions;
    }
}
