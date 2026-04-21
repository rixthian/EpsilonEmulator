namespace Epsilon.CoreGame;

public sealed class HotelSessionSnapshotService : IHotelSessionSnapshotService
{
    private readonly IHotelBootstrapService _hotelBootstrapService;
    private readonly IWalletRepository _walletRepository;
    private readonly IMessengerRepository _messengerRepository;
    private readonly IBadgeRepository _badgeRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IChatCommandRepository _chatCommandRepository;

    public HotelSessionSnapshotService(
        IHotelBootstrapService hotelBootstrapService,
        IWalletRepository walletRepository,
        IMessengerRepository messengerRepository,
        IBadgeRepository badgeRepository,
        IAchievementRepository achievementRepository,
        IChatCommandRepository chatCommandRepository)
    {
        _hotelBootstrapService = hotelBootstrapService;
        _walletRepository = walletRepository;
        _messengerRepository = messengerRepository;
        _badgeRepository = badgeRepository;
        _achievementRepository = achievementRepository;
        _chatCommandRepository = chatCommandRepository;
    }

    public async ValueTask<HotelSessionSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        HotelBootstrapSnapshot? bootstrap = await _hotelBootstrapService.BuildAsync(characterId, cancellationToken);

        if (bootstrap is null)
        {
            return null;
        }

        WalletSnapshot wallet =
            await _walletRepository.GetByCharacterIdAsync(characterId, cancellationToken)
            ?? new WalletSnapshot(characterId, [], []);

        IReadOnlyList<MessengerContact> contacts =
            await _messengerRepository.GetContactsByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<MessengerRequest> requests =
            await _messengerRepository.GetPendingRequestsByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<BadgeAssignment> badges =
            await _badgeRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<AchievementProgress> achievements =
            await _achievementRepository.GetByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<ChatCommandDefinition> commands =
            await _chatCommandRepository.GetAvailableByCharacterIdAsync(characterId, cancellationToken);

        return new HotelSessionSnapshot(
            bootstrap,
            wallet,
            contacts,
            requests,
            badges,
            achievements,
            commands);
    }
}
