using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresCharacterProfileRepository : ICharacterProfileRepository
{
    private const string SqlById = """
        SELECT
            character_id,
            account_id,
            public_id,
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
            public_id,
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

    private const string SqlByPublicId = """
        SELECT
            character_id,
            account_id,
            public_id,
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
        WHERE lower(public_id) = lower(@public_id)
        LIMIT 1;
        """;

    private const string SqlSpendDailyRespect = """
        UPDATE characters
        SET daily_respect_points = daily_respect_points - 1
        WHERE character_id = @character_id
          AND daily_respect_points > 0
        RETURNING
            character_id,
            account_id,
            public_id,
            username,
            motto,
            figure,
            gender,
            home_room_id,
            credits_balance,
            activity_points_balance,
            respect_points,
            daily_respect_points,
            daily_pet_respect_points;
        """;

    private const string SqlUpdate = """
        UPDATE characters
        SET
            motto = @motto,
            figure = @figure,
            gender = @gender,
            public_id = @public_id,
            home_room_id = @home_room_id,
            credits_balance = @credits_balance,
            activity_points_balance = @activity_points_balance,
            respect_points = @respect_points,
            daily_respect_points = @daily_respect_points,
            daily_pet_respect_points = @daily_pet_respect_points
        WHERE character_id = @character_id;
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

    public async ValueTask<CharacterProfile?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SqlByPublicId, connection);
        command.Parameters.AddWithValue("public_id", publicId);
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
            Username: reader.GetString(3),
            Motto: reader.GetString(4),
            Figure: reader.GetString(5),
            Gender: reader.GetString(6),
            HomeRoomId: new RoomId(reader.GetInt64(7)),
            CreditsBalance: reader.GetInt32(8),
            ActivityPointsBalance: reader.GetInt32(9),
            RespectPoints: reader.GetInt32(10),
            DailyRespectPoints: reader.GetInt32(11),
            DailyPetRespectPoints: reader.GetInt32(12),
            PublicId: reader.GetString(2));
    }

    public ValueTask<CharacterProfile> CreateAsync(
        AccountId accountId,
        string username,
        RoomId homeRoomId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Postgres-backed character creation is not implemented yet.");
    }

    public async ValueTask StoreAsync(CharacterProfile profile, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SqlUpdate, connection);
        command.Parameters.AddWithValue("character_id", profile.CharacterId.Value);
        command.Parameters.AddWithValue("motto", profile.Motto);
        command.Parameters.AddWithValue("figure", profile.Figure);
        command.Parameters.AddWithValue("gender", profile.Gender);
        command.Parameters.AddWithValue("public_id", profile.PublicId);
        command.Parameters.AddWithValue("home_room_id", profile.HomeRoomId.Value);
        command.Parameters.AddWithValue("credits_balance", profile.CreditsBalance);
        command.Parameters.AddWithValue("activity_points_balance", profile.ActivityPointsBalance);
        command.Parameters.AddWithValue("respect_points", profile.RespectPoints);
        command.Parameters.AddWithValue("daily_respect_points", profile.DailyRespectPoints);
        command.Parameters.AddWithValue("daily_pet_respect_points", profile.DailyPetRespectPoints);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask<CharacterProfile?> TrySpendDailyRespectAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SqlSpendDailyRespect, connection);
        command.Parameters.AddWithValue("character_id", characterId.Value);
        return await ReadProfileAsync(command, cancellationToken);
    }
}
