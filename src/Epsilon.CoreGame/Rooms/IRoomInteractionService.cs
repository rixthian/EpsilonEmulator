namespace Epsilon.CoreGame;

public interface IRoomInteractionService
{
    ValueTask<RoomActorMovementResult> MoveActorAsync(
        RoomActorMovementRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<RoomChatResult> SendChatAsync(
        RoomChatRequest request,
        CancellationToken cancellationToken = default);
}
