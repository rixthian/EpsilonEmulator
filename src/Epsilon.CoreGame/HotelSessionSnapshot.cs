namespace Epsilon.CoreGame;

public sealed record HotelSessionSnapshot(
    HotelBootstrapSnapshot Bootstrap,
    WalletSnapshot Wallet,
    IReadOnlyList<MessengerContact> MessengerContacts,
    IReadOnlyList<MessengerRequest> PendingMessengerRequests,
    IReadOnlyList<BadgeAssignment> ActiveBadges,
    IReadOnlyList<AchievementProgress> AchievementProgress,
    IReadOnlyList<ChatCommandDefinition> AvailableChatCommands);
