namespace Epsilon.CoreGame;

public sealed record RoomRuntimeCoordinationSnapshot(
    RoomId RoomId,
    long Version,
    RoomRuntimeMutationKind LastMutationKind,
    DateTime LastMutationAtUtc,
    int ActorCount,
    int PlayerCount);
