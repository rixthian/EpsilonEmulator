using Epsilon.Content;
using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresItemDefinitionRepository : IItemDefinitionRepository
{
    private const string Sql = """
        SELECT
            item_definition_id,
            public_name,
            internal_name,
            item_type_code,
            sprite_id,
            stack_height,
            can_stack,
            can_sit,
            is_walkable,
            allow_recycle,
            allow_trade,
            allow_marketplace_sell,
            allow_gift,
            allow_inventory_stack,
            interaction_type_code,
            interaction_modes_count
        FROM item_definitions
        WHERE item_definition_id = @item_definition_id;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresItemDefinitionRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<ItemDefinition?> GetByIdAsync(ItemDefinitionId itemDefinitionId, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("item_definition_id", itemDefinitionId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ItemDefinition(
            ItemDefinitionId: new ItemDefinitionId(reader.GetInt64(0)),
            PublicName: reader.GetString(1),
            InternalName: reader.GetString(2),
            ItemTypeCode: reader.GetString(3),
            SpriteId: reader.GetInt32(4),
            StackHeight: reader.GetDouble(5),
            CanStack: reader.GetBoolean(6),
            CanSit: reader.GetBoolean(7),
            IsWalkable: reader.GetBoolean(8),
            AllowRecycle: reader.GetBoolean(9),
            AllowTrade: reader.GetBoolean(10),
            AllowMarketplaceSell: reader.GetBoolean(11),
            AllowGift: reader.GetBoolean(12),
            AllowInventoryStack: reader.GetBoolean(13),
            InteractionTypeCode: reader.GetString(14),
            InteractionModesCount: reader.GetInt32(15));
    }
}
