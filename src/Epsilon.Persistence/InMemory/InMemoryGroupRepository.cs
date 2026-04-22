using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryGroupRepository : IGroupRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryGroupRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<HotelGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<HotelGroup>>(_store.Groups.Values.ToArray());
        }
    }

    public ValueTask<HotelGroup?> GetByIdAsync(GroupId groupId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.Groups.TryGetValue(groupId, out HotelGroup? group);
            return ValueTask.FromResult(group);
        }
    }

    public ValueTask<HotelGroup?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            HotelGroup? group = _store.Groups.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            return ValueTask.FromResult(group);
        }
    }

    public ValueTask<HotelGroup> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            GroupId groupId = new(_store.NextGroupId++);
            HotelGroup group = new(
                groupId,
                request.OwnerCharacterId,
                request.Name.Trim(),
                request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.BadgeCode) ? null : request.BadgeCode.Trim(),
                request.RoomId,
                request.JoinMode,
                DateTime.UtcNow);

            _store.Groups[groupId] = group;
            _store.GroupMemberships[groupId] = [];
            return ValueTask.FromResult(group);
        }
    }

    public ValueTask<HotelGroup> UpdateAsync(HotelGroup group, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.Groups[group.GroupId] = group;
            return ValueTask.FromResult(group);
        }
    }

    public ValueTask<IReadOnlyList<GroupMembership>> GetMembershipsByGroupIdAsync(
        GroupId groupId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<GroupMembership> memberships =
                _store.GroupMemberships.TryGetValue(groupId, out List<GroupMembership>? list)
                    ? [.. list]
                    : [];

            return ValueTask.FromResult(memberships);
        }
    }

    public ValueTask<IReadOnlyList<GroupMembership>> GetMembershipsByCharacterIdAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<GroupMembership> memberships = _store.GroupMemberships.Values
                .SelectMany(static list => list)
                .Where(candidate => candidate.CharacterId == characterId)
                .ToArray();

            return ValueTask.FromResult(memberships);
        }
    }

    public ValueTask<GroupMembership?> GetMembershipAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            GroupMembership? membership =
                _store.GroupMemberships.TryGetValue(groupId, out List<GroupMembership>? list)
                    ? list.FirstOrDefault(candidate => candidate.CharacterId == characterId)
                    : null;

            return ValueTask.FromResult(membership);
        }
    }

    public ValueTask<GroupMembership> UpsertMembershipAsync(
        GroupMembership membership,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.GroupMemberships.TryGetValue(membership.GroupId, out List<GroupMembership>? list))
            {
                list = [];
                _store.GroupMemberships[membership.GroupId] = list;
            }

            int existingIndex = list.FindIndex(candidate => candidate.CharacterId == membership.CharacterId);
            if (existingIndex >= 0)
            {
                list[existingIndex] = membership;
            }
            else
            {
                list.Add(membership);
            }

            return ValueTask.FromResult(membership);
        }
    }

    public ValueTask<bool> RemoveMembershipAsync(
        GroupId groupId,
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_store.GroupMemberships.TryGetValue(groupId, out List<GroupMembership>? list))
            {
                return ValueTask.FromResult(false);
            }

            int removed = list.RemoveAll(candidate => candidate.CharacterId == characterId);
            return ValueTask.FromResult(removed > 0);
        }
    }
}
