using Epsilon.CoreGame;

namespace Epsilon.Launcher;

public sealed record LauncherSessionSnapshot(
    long AccountId,
    long CharacterId,
    string Ticket,
    DateTime ExpiresAtUtc,
    CollectorProfileSnapshot? Collector,
    LaunchEntitlementSnapshot? LaunchEntitlement);
