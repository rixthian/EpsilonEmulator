using Epsilon.CoreGame;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresPetProfileRepository : IPetProfileRepository
{
    private const string Sql = """
        SELECT
            pet_id,
            owner_character_id,
            room_id,
            name,
            pet_type_id,
            race_code,
            color_code,
            experience,
            energy,
            nutrition,
            respect,
            level
        FROM pets
        WHERE owner_character_id = @owner_character_id
        ORDER BY pet_id;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresPetProfileRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<IReadOnlyList<PetProfile>> GetByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        List<PetProfile> pets = [];

        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(Sql, connection);
        command.Parameters.AddWithValue("owner_character_id", characterId.Value);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            pets.Add(new PetProfile(
                PetId: new PetId(reader.GetInt64(0)),
                OwnerCharacterId: new CharacterId(reader.GetInt64(1)),
                CurrentRoomId: new RoomId(reader.GetInt64(2)),
                Name: reader.GetString(3),
                PetTypeId: reader.GetInt32(4),
                RaceCode: reader.GetString(5),
                ColorCode: reader.GetString(6),
                Experience: reader.GetInt32(7),
                Energy: reader.GetInt32(8),
                Nutrition: reader.GetInt32(9),
                Respect: reader.GetInt32(10),
                Level: reader.GetInt32(11)));
        }

        return pets;
    }
}
