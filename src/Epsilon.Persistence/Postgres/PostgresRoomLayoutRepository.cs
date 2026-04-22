using Epsilon.Rooms;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresRoomLayoutRepository : IRoomLayoutRepository
{
    private const string Sql = """
        SELECT
            layout_code,
            door_x,
            door_y,
            door_z,
            door_rotation,
            heightmap,
            public_room_object_sets::text,
            club_only
        FROM room_layouts
        WHERE layout_code = @layout_code;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresRoomLayoutRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<RoomLayoutDefinition?> GetByCodeAsync(string layoutCode, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("layout_code", layoutCode);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new RoomLayoutDefinition(
            LayoutCode: reader.GetString(0),
            DoorPosition: new FloorPosition(
                X: reader.GetInt32(1),
                Y: reader.GetInt32(2),
                Z: reader.GetDouble(3)),
            DoorRotation: reader.GetInt32(4),
            Heightmap: reader.GetString(5),
            PublicRoomObjectSetCodes: PostgresModelValueMapper.ParseStringList(reader.IsDBNull(6) ? null : reader.GetString(6)),
            ClubOnly: reader.GetBoolean(7));
    }
}
