namespace Epsilon.CoreGame;

public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _groups;
    private readonly ICharacterProfileRepository _characterProfiles;
    private readonly Epsilon.Rooms.IRoomRepository _rooms;

    public GroupService(
        IGroupRepository groups,
        ICharacterProfileRepository characterProfiles,
        Epsilon.Rooms.IRoomRepository rooms)
    {
        _groups = groups;
        _characterProfiles = characterProfiles;
        _rooms = rooms;
    }

    public async ValueTask<IReadOnlyList<HotelGroupSummary>> ListAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HotelGroup> groups = await _groups.GetAllAsync(cancellationToken);
        List<HotelGroupSummary> result = [];

        foreach (HotelGroup group in groups.OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(await BuildSummaryAsync(group, viewerCharacterId, cancellationToken));
        }

        return result;
    }

    public async ValueTask<HotelGroupSnapshot?> GetAsync(
        GroupId groupId,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default)
    {
        HotelGroup? group = await _groups.GetByIdAsync(groupId, cancellationToken);
        return group is null ? null : await BuildSnapshotAsync(group, viewerCharacterId, cancellationToken);
    }

    public async ValueTask<CreateGroupResult> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateGroupResult(false, "name_required", null);
        }

        CharacterProfile? owner = await _characterProfiles.GetByIdAsync(request.OwnerCharacterId, cancellationToken);
        if (owner is null)
        {
            return new CreateGroupResult(false, "owner_not_found", null);
        }

        HotelGroup? existing = await _groups.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
        {
            return new CreateGroupResult(false, "name_taken", null);
        }

        HotelGroup group = await _groups.CreateAsync(request, cancellationToken);
        await _groups.UpsertMembershipAsync(
            new GroupMembership(
                group.GroupId,
                request.OwnerCharacterId,
                GroupMemberRole.Owner,
                DateTime.UtcNow,
                false),
            cancellationToken);

        HotelGroupSnapshot snapshot = await BuildSnapshotAsync(group, request.OwnerCharacterId, cancellationToken);
        return new CreateGroupResult(true, null, snapshot);
    }

    public async ValueTask<HotelGroupSnapshot?> JoinAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        HotelGroup? group = await _groups.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(characterId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        GroupMembership? existingMembership = await _groups.GetMembershipAsync(groupId, characterId, cancellationToken);
        if (existingMembership is null)
        {
            GroupMemberRole role = group.JoinMode == GroupJoinMode.Open
                ? GroupMemberRole.Member
                : GroupMemberRole.Pending;

            await _groups.UpsertMembershipAsync(
                new GroupMembership(groupId, characterId, role, DateTime.UtcNow, false),
                cancellationToken);
        }

        return await BuildSnapshotAsync(group, characterId, cancellationToken);
    }

    public async ValueTask<bool> LeaveAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        GroupMembership? membership = await _groups.GetMembershipAsync(groupId, characterId, cancellationToken);
        if (membership is null || membership.Role == GroupMemberRole.Owner)
        {
            return false;
        }

        return await _groups.RemoveMembershipAsync(groupId, characterId, cancellationToken);
    }

    public async ValueTask<HotelGroupSnapshot?> SetLinkedRoomAsync(
        GroupId groupId,
        CharacterId characterId,
        RoomId? roomId,
        CancellationToken cancellationToken = default)
    {
        HotelGroup? group = await _groups.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        GroupMembership? membership = await _groups.GetMembershipAsync(groupId, characterId, cancellationToken);
        if (membership is null || membership.Role is not (GroupMemberRole.Owner or GroupMemberRole.Admin))
        {
            return null;
        }

        if (roomId is not null)
        {
            Epsilon.Rooms.RoomDefinition? room = await _rooms.GetByIdAsync(roomId.Value, cancellationToken);
            if (room is null ||
                room.RoomKind != RoomKind.Private ||
                room.OwnerCharacterId != characterId)
            {
                return null;
            }
        }

        HotelGroup updatedGroup = group with { RoomId = roomId };
        await _groups.UpdateAsync(updatedGroup, cancellationToken);
        return await BuildSnapshotAsync(updatedGroup, characterId, cancellationToken);
    }

    public async ValueTask<IReadOnlyList<HotelGroupSummary>> GetMembershipsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GroupMembership> memberships =
            await _groups.GetMembershipsByCharacterIdAsync(characterId, cancellationToken);

        List<HotelGroupSummary> result = [];
        foreach (GroupMembership membership in memberships)
        {
            HotelGroup? group = await _groups.GetByIdAsync(membership.GroupId, cancellationToken);
            if (group is not null)
            {
                result.Add(await BuildSummaryAsync(group, characterId, cancellationToken));
            }
        }

        return result;
    }

    private async ValueTask<HotelGroupSummary> BuildSummaryAsync(
        HotelGroup group,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GroupMembership> memberships =
            await _groups.GetMembershipsByGroupIdAsync(group.GroupId, cancellationToken);
        GroupMembership? viewerMembership = memberships.FirstOrDefault(candidate => candidate.CharacterId == viewerCharacterId);

        return new HotelGroupSummary(
            group,
            memberships.Count(candidate => candidate.Role != GroupMemberRole.Pending),
            viewerMembership?.Role,
            viewerMembership is not null,
            viewerMembership is not null && (viewerMembership.Role is GroupMemberRole.Owner or GroupMemberRole.Admin));
    }

    private async ValueTask<HotelGroupSnapshot> BuildSnapshotAsync(
        HotelGroup group,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GroupMembership> memberships =
            await _groups.GetMembershipsByGroupIdAsync(group.GroupId, cancellationToken);
        List<GroupMemberSnapshot> members = [];

        foreach (GroupMembership membership in memberships.OrderBy(candidate => candidate.Role).ThenBy(candidate => candidate.JoinedAtUtc))
        {
            CharacterProfile? profile = await _characterProfiles.GetByIdAsync(membership.CharacterId, cancellationToken);
            if (profile is null)
            {
                continue;
            }

            members.Add(new GroupMemberSnapshot(
                membership.CharacterId,
                profile.Username,
                profile.Figure,
                profile.Motto,
                membership.Role,
                membership.JoinedAtUtc,
                membership.IsFavourite));
        }

        GroupMembership? viewerMembership = memberships.FirstOrDefault(candidate => candidate.CharacterId == viewerCharacterId);
        int memberCount = memberships.Count(candidate => candidate.Role != GroupMemberRole.Pending);

        return new HotelGroupSnapshot(
            group,
            members,
            memberCount,
            viewerMembership?.Role,
            viewerMembership is not null,
            viewerMembership is not null && (viewerMembership.Role is GroupMemberRole.Owner or GroupMemberRole.Admin));
    }
}
