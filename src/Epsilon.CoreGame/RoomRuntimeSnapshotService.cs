namespace Epsilon.CoreGame;

public sealed class RoomRuntimeSnapshotService : IRoomRuntimeSnapshotService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;

    public RoomRuntimeSnapshotService(
        IHotelReadService hotelReadService,
        IRoomRuntimeRepository roomRuntimeRepository)
    {
        _hotelReadService = hotelReadService;
        _roomRuntimeRepository = roomRuntimeRepository;
    }

    public async ValueTask<RoomRuntimeSnapshot?> BuildAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(roomId, cancellationToken);

        if (room is null)
        {
            return null;
        }

        IReadOnlyList<RoomActorState> actors = await _roomRuntimeRepository.GetActorsByRoomIdAsync(roomId, cancellationToken);
        RoomActivitySnapshot? activity = await _roomRuntimeRepository.GetActivityByRoomIdAsync(roomId, cancellationToken);
        RoomChatPolicySnapshot chatPolicy =
            await _roomRuntimeRepository.GetChatPolicyByRoomIdAsync(roomId, cancellationToken)
            ?? new RoomChatPolicySnapshot(false, false, 100, 4, 4);
        IReadOnlyList<RoomChatMessage> chatMessages =
            await _roomRuntimeRepository.GetChatMessagesByRoomIdAsync(roomId, cancellationToken);

        return new RoomRuntimeSnapshot(room, actors, activity, chatPolicy, chatMessages);
    }
}
