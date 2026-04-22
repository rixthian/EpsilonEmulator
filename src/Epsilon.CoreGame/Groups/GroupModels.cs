namespace Epsilon.CoreGame;

public enum GroupJoinMode
{
    Open = 0,
    Locked = 1,
    Private = 2
}

public enum GroupMemberRole
{
    Owner = 0,
    Admin = 1,
    Member = 2,
    Pending = 3
}

public sealed record HotelGroup(
    GroupId GroupId,
    CharacterId OwnerCharacterId,
    string Name,
    string Description,
    string? BadgeCode,
    RoomId? RoomId,
    GroupJoinMode JoinMode,
    DateTime CreatedAtUtc);

public sealed record GroupMembership(
    GroupId GroupId,
    CharacterId CharacterId,
    GroupMemberRole Role,
    DateTime JoinedAtUtc,
    bool IsFavourite);

public sealed record GroupMemberSnapshot(
    CharacterId CharacterId,
    string Username,
    string Figure,
    string Motto,
    GroupMemberRole Role,
    DateTime JoinedAtUtc,
    bool IsFavourite);

public sealed record HotelGroupSummary(
    HotelGroup Group,
    int MemberCount,
    GroupMemberRole? ViewerRole,
    bool IsJoined,
    bool CanManage);

public sealed record HotelGroupSnapshot(
    HotelGroup Group,
    IReadOnlyList<GroupMemberSnapshot> Members,
    int MemberCount,
    GroupMemberRole? ViewerRole,
    bool IsJoined,
    bool CanManage);

public sealed record CreateGroupRequest(
    CharacterId OwnerCharacterId,
    string Name,
    string Description,
    string? BadgeCode,
    RoomId? RoomId,
    GroupJoinMode JoinMode);

public sealed record CreateGroupResult(
    bool Succeeded,
    string? FailureCode,
    HotelGroupSnapshot? Snapshot);
