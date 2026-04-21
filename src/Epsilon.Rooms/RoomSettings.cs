using Epsilon.CoreGame;

namespace Epsilon.Rooms;

public sealed record RoomSettings(
    RoomAccessMode AccessMode,
    string? Password,
    int MaximumUsers,
    bool AllowPets,
    bool AllowPetEating,
    bool AllowWalkThrough,
    bool HideWalls);

