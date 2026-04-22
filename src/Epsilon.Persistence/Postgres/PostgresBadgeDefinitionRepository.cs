using Epsilon.Content;
using Npgsql;

namespace Epsilon.Persistence;

internal sealed class PostgresBadgeDefinitionRepository : IBadgeDefinitionRepository
{
    private const string GetByCodeSql = """
        SELECT badge_code, badge_name, badge_group, required_right, asset_path, asset_kind
        FROM badge_definitions
        WHERE badge_code = @badge_code;
        """;

    private const string SearchSql = """
        SELECT badge_code, badge_name, badge_group, required_right, asset_path, asset_kind
        FROM badge_definitions
        WHERE @query IS NULL
           OR badge_code ILIKE @pattern
           OR badge_name ILIKE @pattern
           OR COALESCE(badge_group, '') ILIKE @pattern
        ORDER BY badge_code
        LIMIT @take;
        """;

    private readonly PostgresDataSourceProvider _dataSourceProvider;

    public PostgresBadgeDefinitionRepository(PostgresDataSourceProvider dataSourceProvider)
    {
        _dataSourceProvider = dataSourceProvider;
    }

    public async ValueTask<BadgeDefinition?> GetByCodeAsync(
        string badgeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(badgeCode))
        {
            return null;
        }

        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(GetByCodeSql, connection);
        command.Parameters.AddWithValue("badge_code", badgeCode.Trim());

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async ValueTask<IReadOnlyList<BadgeDefinition>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await _dataSourceProvider.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = new(SearchSql, connection);
        string? normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        command.Parameters.AddWithValue("query", normalizedQuery is null ? DBNull.Value : normalizedQuery);
        command.Parameters.AddWithValue("pattern", normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%");
        command.Parameters.AddWithValue("take", take);

        List<BadgeDefinition> results = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    private static BadgeDefinition Map(NpgsqlDataReader reader)
    {
        return new BadgeDefinition(
            BadgeCode: reader.GetString(0),
            BadgeName: reader.GetString(1),
            BadgeGroup: reader.IsDBNull(2) ? null : reader.GetString(2),
            RequiredRight: reader.IsDBNull(3) ? null : reader.GetString(3),
            AssetPath: reader.GetString(4),
            AssetKind: reader.GetString(5));
    }
}
