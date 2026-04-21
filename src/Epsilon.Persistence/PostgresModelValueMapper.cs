using System.Text.Json;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal static class PostgresModelValueMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        string[]? values = JsonSerializer.Deserialize<string[]>(json, JsonOptions);
        return values ?? [];
    }

    public static SubscriptionType ParseSubscriptionType(string rawValue)
    {
        return rawValue.Trim().ToUpperInvariant() switch
        {
            "CLUB" => SubscriptionType.Club,
            "VIP" => SubscriptionType.Vip,
            _ => throw new InvalidOperationException($"Unsupported subscription type '{rawValue}'.")
        };
    }

    public static RoomKind ParseRoomKind(string rawValue)
    {
        return rawValue.Trim().ToUpperInvariant() switch
        {
            "PRIVATE" => RoomKind.Private,
            "PUBLIC" => RoomKind.Public,
            _ => throw new InvalidOperationException($"Unsupported room kind '{rawValue}'.")
        };
    }

    public static RoomAccessMode ParseRoomAccessMode(string rawValue)
    {
        return rawValue.Trim().ToUpperInvariant() switch
        {
            "OPEN" => RoomAccessMode.Open,
            "LOCKED" => RoomAccessMode.Locked,
            "PASSWORD" => RoomAccessMode.PasswordProtected,
            "PASSWORDPROTECTED" => RoomAccessMode.PasswordProtected,
            _ => throw new InvalidOperationException($"Unsupported room access mode '{rawValue}'.")
        };
    }
}
