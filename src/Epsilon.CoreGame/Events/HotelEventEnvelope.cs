namespace Epsilon.CoreGame;

public sealed record HotelEventEnvelope(
    Guid EventId,
    HotelEventKind Kind,
    DateTime OccurredAtUtc,
    CharacterId? ActorCharacterId,
    RoomId? RoomId,
    object Payload);
