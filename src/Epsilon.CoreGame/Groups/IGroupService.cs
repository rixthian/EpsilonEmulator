namespace Epsilon.CoreGame;

public interface IGroupService
{
    ValueTask<IReadOnlyList<HotelGroupSummary>> ListAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default);

    ValueTask<HotelGroupSnapshot?> GetAsync(
        GroupId groupId,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default);

    ValueTask<CreateGroupResult> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<HotelGroupSnapshot?> JoinAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> LeaveAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<HotelGroupSnapshot?> SetLinkedRoomAsync(
        GroupId groupId,
        CharacterId characterId,
        RoomId? roomId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HotelGroupSummary>> GetMembershipsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
