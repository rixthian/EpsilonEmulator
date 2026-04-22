using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class RoomEntryService : IRoomEntryService
{
    private readonly IHotelReadService _hotelReadService;
    private readonly INavigatorPublicRoomRepository _navigatorPublicRoomRepository;
    private readonly IRoomBotRuntimeService _roomBotRuntimeService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IRoomRuntimeCoordinator _roomRuntimeCoordinator;
    private readonly IModerationRepository _moderationRepository;
    private readonly IHotelOperationalState _hotelOperationalState;

    public RoomEntryService(
        IHotelReadService hotelReadService,
        INavigatorPublicRoomRepository navigatorPublicRoomRepository,
        IRoomBotRuntimeService roomBotRuntimeService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IRoomRuntimeCoordinator roomRuntimeCoordinator,
        IModerationRepository moderationRepository,
        IHotelOperationalState hotelOperationalState)
    {
        _hotelReadService = hotelReadService;
        _navigatorPublicRoomRepository = navigatorPublicRoomRepository;
        _roomBotRuntimeService = roomBotRuntimeService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _roomRuntimeCoordinator = roomRuntimeCoordinator;
        _moderationRepository = moderationRepository;
        _hotelOperationalState = hotelOperationalState;
    }

    public async ValueTask<RoomEntryResult> EnterAsync(
        RoomEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        List<RoomEntryStageSnapshot> stages =
        [
            new(RoomEntryStage.Requested, true, $"Room entry requested for character {request.CharacterId.Value} and room {request.RoomId.Value}.")
        ];

        // Lockdown check runs before any other validation so staff-initiated
        // lockdowns take effect immediately regardless of room or character state.
        if (_hotelOperationalState.IsLockdownActive)
        {
            string lockdownDetail = string.IsNullOrWhiteSpace(_hotelOperationalState.LockdownMessage)
                ? "The hotel is temporarily locked down. Room entry is not permitted."
                : _hotelOperationalState.LockdownMessage;

            return Fail(stages, RoomEntryFailureCode.HotelLockdown, lockdownDetail);
        }

        CharacterHotelSnapshot? character = await _hotelReadService.GetCharacterSnapshotAsync(
            request.CharacterId,
            cancellationToken);

        if (character is null)
        {
            return Fail(
                stages,
                RoomEntryFailureCode.CharacterNotFound,
                "Character could not be resolved.");
        }

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.CharacterResolved,
            true,
            $"Character '{character.Profile.Username}' was resolved."));

        ModerationBanRecord? activeBan =
            await _moderationRepository.GetActiveBanByCharacterIdAsync(request.CharacterId, cancellationToken);
        if (activeBan is not null)
        {
            return Fail(
                stages,
                RoomEntryFailureCode.Banned,
                activeBan.IsPermanent
                    ? $"Character '{character.Profile.Username}' is permanently banned. Reason: {activeBan.Reason}"
                    : $"Character '{character.Profile.Username}' is banned until {activeBan.ExpiresAtUtc:O}. Reason: {activeBan.Reason}");
        }

        RoomHotelSnapshot? room = await _hotelReadService.GetRoomSnapshotAsync(
            request.RoomId,
            cancellationToken);

        if (room is null)
        {
            return Fail(
                stages,
                RoomEntryFailureCode.RoomNotFound,
                "Room could not be resolved.");
        }

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.RoomResolved,
            true,
            $"Room '{room.Room.Name}' was resolved."));

        bool canManageRoom = room.Room.OwnerCharacterId == request.CharacterId;
        RoomEntryResult? accessFailure = ValidateAccess(request, character, room, canManageRoom, stages);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.AccessValidated,
            true,
            "Room access was validated."));

        if (room.Layout is null)
        {
            return Fail(
                stages,
                RoomEntryFailureCode.MissingLayout,
                "Room layout is missing.");
        }

        if (room.Layout.ClubOnly && !HasClubAccess(character))
        {
            return Fail(
                stages,
                RoomEntryFailureCode.LayoutRestricted,
                "Room layout requires an active club-compatible subscription.");
        }

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.LayoutValidated,
            true,
            $"Layout '{room.Layout.LayoutCode}' was validated."));

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.ContentPrepared,
            true,
            $"{room.Items.Count} room items were prepared."));

        NavigatorPublicRoomDefinition? publicRoomEntry = null;
        if (room.Room.RoomKind == RoomKind.Public)
        {
            publicRoomEntry = await _navigatorPublicRoomRepository.GetByRoomIdAsync(
                room.Room.RoomId,
                cancellationToken);
        }

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.ContextPrepared,
            true,
            publicRoomEntry is null
                ? "Entry context prepared without public-room navigator metadata."
                : $"Public-room entry '{publicRoomEntry.Caption}' was linked."));

        if (!request.SpectatorMode)
        {
            IReadOnlyList<RoomId> removedRoomIds = await _roomRuntimeRepository.RemoveActorFromAllRoomsAsync(
                request.CharacterId.Value,
                cancellationToken);

            RoomActorState actorState = BuildEnteredActorState(character.Profile, room.Layout);
            await _roomRuntimeRepository.StoreActorStateAsync(request.RoomId, actorState, cancellationToken);

            foreach (RoomId removedRoomId in removedRoomIds.Where(candidate => candidate != request.RoomId))
            {
                await _roomRuntimeCoordinator.SignalMutationAsync(
                    removedRoomId,
                    RoomRuntimeMutationKind.ActorPresenceChanged,
                    cancellationToken);
            }

            await _roomRuntimeCoordinator.SignalMutationAsync(
                request.RoomId,
                RoomRuntimeMutationKind.ActorPresenceChanged,
                cancellationToken);

            if (publicRoomEntry is not null)
            {
                IReadOnlyList<RoomActorState> spawnedBots = await _roomBotRuntimeService.EnsurePublicRoomBotsAsync(
                    publicRoomEntry,
                    room.Layout,
                    cancellationToken);

                if (spawnedBots.Count > 0)
                {
                    stages.Add(new RoomEntryStageSnapshot(
                        RoomEntryStage.ContextPrepared,
                        true,
                        $"{spawnedBots.Count} public-room bot actor(s) are active."));
                }
            }

            stages.Add(new RoomEntryStageSnapshot(
                RoomEntryStage.RuntimePresenceRegistered,
                true,
                removedRoomIds.Count == 0
                    ? "Room runtime presence was registered."
                    : $"Room runtime presence was registered and actor state was migrated from {removedRoomIds.Count} room slot(s)."));
        }

        RoomEntrySnapshot snapshot = new(
            character,
            room,
            publicRoomEntry,
            canManageRoom,
            request.SpectatorMode);

        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.Completed,
            true,
            "Room entry flow completed."));

        return new RoomEntryResult(
            true,
            null,
            null,
            stages,
            snapshot);
    }

    private static RoomActorState BuildEnteredActorState(
        CharacterProfile character,
        Epsilon.Rooms.RoomLayoutDefinition layout)
    {
        RoomCoordinate doorCoordinate = new(
            layout.DoorPosition.X,
            layout.DoorPosition.Y,
            layout.DoorPosition.Z);

        return new RoomActorState(
            ActorId: character.CharacterId.Value,
            ActorKind: RoomActorKind.Player,
            DisplayName: character.Username,
            Position: doorCoordinate,
            BodyRotation: layout.DoorRotation,
            HeadRotation: layout.DoorRotation,
            IsTyping: false,
            IsWalking: false,
            IsSitting: false,
            IsLaying: false,
            CarryItem: null,
            Goal: null,
            StatusEntries: []);
    }

    private static RoomEntryResult? ValidateAccess(
        RoomEntryRequest request,
        CharacterHotelSnapshot character,
        RoomHotelSnapshot room,
        bool canManageRoom,
        List<RoomEntryStageSnapshot> stages)
    {
        if (room.Room.RoomKind == RoomKind.Public || canManageRoom)
        {
            return null;
        }

        switch (room.Room.Settings.AccessMode)
        {
            case RoomAccessMode.Open:
                return null;

            case RoomAccessMode.Locked:
                return Fail(
                    stages,
                    RoomEntryFailureCode.AccessDenied,
                    $"Character '{character.Profile.Username}' does not have access to this locked room.");

            case RoomAccessMode.PasswordProtected:
                if (string.Equals(
                        room.Room.Settings.Password ?? string.Empty,
                        request.Password ?? string.Empty,
                        StringComparison.Ordinal))
                {
                    return null;
                }

                return Fail(
                    stages,
                    RoomEntryFailureCode.InvalidPassword,
                    "The provided room password is invalid.");

            default:
                return Fail(
                    stages,
                    RoomEntryFailureCode.AccessDenied,
                    "Room access mode is not supported.");
        }
    }

    private static bool HasClubAccess(CharacterHotelSnapshot character)
    {
        DateTime utcNow = DateTime.UtcNow;

        return character.Subscriptions.Any(subscription =>
            subscription.ExpiresAtUtc > utcNow &&
            (subscription.SubscriptionType == SubscriptionType.Club || subscription.SubscriptionType == SubscriptionType.Vip));
    }

    private static RoomEntryResult Fail(
        List<RoomEntryStageSnapshot> stages,
        RoomEntryFailureCode failureCode,
        string failureDetail)
    {
        stages.Add(new RoomEntryStageSnapshot(
            RoomEntryStage.Failed,
            false,
            failureDetail));

        return new RoomEntryResult(
            false,
            failureCode,
            failureDetail,
            stages,
            null);
    }
}
