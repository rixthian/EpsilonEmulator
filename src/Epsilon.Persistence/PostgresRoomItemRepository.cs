using Epsilon.CoreGame;
using Epsilon.Rooms;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresRoomItemRepository : IRoomItemRepository
{
    private const string Sql = """
        SELECT
            item_id,
            item_definition_id,
            room_id,
            floor_x,
            floor_y,
            floor_z,
            rotation,
            wall_position,
            state_data
        FROM room_items
        WHERE room_id = @room_id
        ORDER BY item_id;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresRoomItemRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<IReadOnlyList<RoomItemState>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        List<RoomItemState> items = [];

        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("room_id", roomId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            FloorPosition? floorPosition = reader.IsDBNull(3)
                ? null
                : new FloorPosition(
                    X: reader.GetInt32(3),
                    Y: reader.GetInt32(4),
                    Z: reader.GetDouble(5));

            WallPosition? wallPosition = reader.IsDBNull(7)
                ? null
                : new WallPosition(reader.GetString(7));

            items.Add(new RoomItemState(
                ItemId: new ItemId(reader.GetInt64(0)),
                ItemDefinitionId: new ItemDefinitionId(reader.GetInt64(1)),
                RoomId: new RoomId(reader.GetInt64(2)),
                Placement: new RoomItemPlacement(
                    FloorPosition: floorPosition,
                    Rotation: reader.GetInt32(6),
                    WallPosition: wallPosition),
                StateData: reader.GetString(8)));
        }

        return items;
    }

    public ValueTask<IReadOnlyList<RoomItemState>> RemoveByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Postgres-backed room-item removal is not wired yet.");
    }
}
