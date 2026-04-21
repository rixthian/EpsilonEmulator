using Epsilon.CoreGame;
using Epsilon.Rooms;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresRoomRepository : IRoomRepository
{
    private const string Sql = """
        SELECT
            room_id,
            room_kind,
            owner_character_id,
            name,
            description,
            category_id,
            layout_code,
            access_mode,
            access_password,
            maximum_users,
            allow_pets,
            allow_pet_eating,
            allow_walk_through,
            hide_walls,
            tags::text
        FROM rooms
        WHERE room_id = @room_id;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresRoomRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<RoomDefinition?> GetByIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("room_id", roomId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        CharacterId? ownerCharacterId = reader.IsDBNull(2)
            ? null
            : new CharacterId(reader.GetInt64(2));

        RoomSettings settings = new(
            AccessMode: PostgresModelValueMapper.ParseRoomAccessMode(reader.GetString(7)),
            Password: reader.IsDBNull(8) ? null : reader.GetString(8),
            MaximumUsers: reader.GetInt32(9),
            AllowPets: reader.GetBoolean(10),
            AllowPetEating: reader.GetBoolean(11),
            AllowWalkThrough: reader.GetBoolean(12),
            HideWalls: reader.GetBoolean(13));

        return new RoomDefinition(
            RoomId: new RoomId(reader.GetInt64(0)),
            RoomKind: PostgresModelValueMapper.ParseRoomKind(reader.GetString(1)),
            OwnerCharacterId: ownerCharacterId,
            Name: reader.GetString(3),
            Description: reader.GetString(4),
            CategoryId: reader.GetInt32(5),
            LayoutCode: reader.GetString(6),
            Settings: settings,
            Tags: PostgresModelValueMapper.ParseStringList(reader.IsDBNull(14) ? null : reader.GetString(14)));
    }
}
