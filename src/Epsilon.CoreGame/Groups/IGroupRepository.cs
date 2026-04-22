namespace Epsilon.CoreGame;

public interface IGroupRepository
{
    ValueTask<IReadOnlyList<HotelGroup>> GetAllAsync(CancellationToken cancellationToken = default);

    ValueTask<HotelGroup?> GetByIdAsync(GroupId groupId, CancellationToken cancellationToken = default);

    ValueTask<HotelGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<HotelGroup> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    ValueTask<HotelGroup> UpdateAsync(HotelGroup group, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<GroupMembership>> GetMembershipsByGroupIdAsync(
        GroupId groupId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<GroupMembership>> GetMembershipsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<GroupMembership?> GetMembershipAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default);

    ValueTask<GroupMembership> UpsertMembershipAsync(
        GroupMembership membership,
        CancellationToken cancellationToken = default);

    ValueTask<bool> RemoveMembershipAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
