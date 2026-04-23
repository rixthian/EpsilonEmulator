namespace Epsilon.CoreGame;

public interface IHotelEventBus
{
    ValueTask PublishAsync(
        HotelEventEnvelope envelope,
        CancellationToken cancellationToken = default);

    ValueTask PublishAsync<TPayload>(
        HotelEventKind kind,
        TPayload payload,
        CharacterId? actorCharacterId = null,
        RoomId? roomId = null,
        CancellationToken cancellationToken = default)
        where TPayload : class;

    IDisposable Subscribe(HotelEventHandler handler);

    ValueTask<IReadOnlyList<HotelEventEnvelope>> GetRecentAsync(
        int maxCount = 128,
        CancellationToken cancellationToken = default);
}
