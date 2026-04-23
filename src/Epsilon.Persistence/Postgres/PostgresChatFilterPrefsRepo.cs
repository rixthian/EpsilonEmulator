using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresCharacterChatFilterPreferenceRepository : ICharacterChatFilterPreferenceRepository
{
    private const string GetByCharacterIdSql = """
        SELECT
            character_id,
            mute_bots,
            mute_pets,
            updated_at_utc,
            updated_by
        FROM character_chat_filter_preferences
        WHERE character_id = @character_id;
        """;

    private const string UpsertSql = """
        INSERT INTO character_chat_filter_preferences (
            character_id,
            mute_bots,
            mute_pets,
            updated_at_utc,
            updated_by
        )
        VALUES (
            @character_id,
            @mute_bots,
            @mute_pets,
            @updated_at_utc,
            @updated_by
        )
        ON CONFLICT (character_id) DO UPDATE
        SET
            mute_bots = EXCLUDED.mute_bots,
            mute_pets = EXCLUDED.mute_pets,
            updated_at_utc = EXCLUDED.updated_at_utc,
            updated_by = EXCLUDED.updated_by;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresCharacterChatFilterPreferenceRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<CharacterChatFilterPreference?> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(GetByCharacterIdSql, connection);
        command.Parameters.AddWithValue("character_id", characterId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterChatFilterPreference(
            CharacterId: new CharacterId(reader.GetInt64(0)),
            MuteBots: reader.GetBoolean(1),
            MutePets: reader.GetBoolean(2),
            UpdatedAt: reader.GetFieldValue<DateTimeOffset>(3),
            UpdatedBy: reader.GetString(4));
    }

    public async ValueTask StoreAsync(
        CharacterChatFilterPreference preference,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(UpsertSql, connection);
        command.Parameters.AddWithValue("character_id", preference.CharacterId.Value);
        command.Parameters.AddWithValue("mute_bots", preference.MuteBots);
        command.Parameters.AddWithValue("mute_pets", preference.MutePets);
        command.Parameters.AddWithValue("updated_at_utc", preference.UpdatedAt);
        command.Parameters.AddWithValue("updated_by", preference.UpdatedBy);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
