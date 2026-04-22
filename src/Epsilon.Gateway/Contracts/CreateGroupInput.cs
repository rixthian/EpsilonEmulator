namespace Epsilon.Gateway;

public sealed record CreateGroupInput(
    string Name,
    string Description,
    string? BadgeCode,
    long? RoomId,
    string JoinMode);
