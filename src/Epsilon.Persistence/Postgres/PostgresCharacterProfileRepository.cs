using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresCharacterProfileRepository : ICharacterProfileRepository
{
    private const string SqlById = """
        SELECT
            character_id,
            account_id,
            username,
            motto,
            figure,
            gender,
            home_room_id,
            credits_balance,
            activity_points_balance,
            respect_points,
            daily_respect_points,
            daily_pet_respect_points
        FROM characters
        WHERE character_id = @character_id;
        """;

    private const string SqlByUsername = """
        SELECT
            character_id,
            account_id,
            username,
            motto,
            figure,
            gender,
            home_room_id,
            credits_balance,
            activity_points_balance,
            respect_points,
            daily_respect_points,
            daily_pet_respect_points
        FROM characters
        WHERE lower(username) = lower(@username)
        LIMIT 1;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresCharacterProfileRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<CharacterProfile?> GetByIdAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SqlById, connection);
        command.Parameters.AddWithValue("character_id", characterId.Value);
        return await ReadProfileAsync(command, cancellationToken);
    }

    public async ValueTask<CharacterProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SqlByUsername, connection);
        command.Parameters.AddWithValue("username", username);
        return await ReadProfileAsync(command, cancellationToken);
    }

    private static async ValueTask<CharacterProfile?> ReadProfileAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterProfile(
            CharacterId: new CharacterId(reader.GetInt64(0)),
            AccountId: new AccountId(reader.GetInt64(1)),
            Username: reader.GetString(2),
            Motto: reader.GetString(3),
            Figure: reader.GetString(4),
            Gender: reader.GetString(5),
            HomeRoomId: new RoomId(reader.GetInt64(6)),
            CreditsBalance: reader.GetInt32(7),
            ActivityPointsBalance: reader.GetInt32(8),
            RespectPoints: reader.GetInt32(9),
            DailyRespectPoints: reader.GetInt32(10),
            DailyPetRespectPoints: reader.GetInt32(11));
    }

    public ValueTask<CharacterProfile> CreateAsync(
        AccountId accountId,
        string username,
        RoomId homeRoomId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Postgres-backed character creation is not implemented yet.");
    }
}
