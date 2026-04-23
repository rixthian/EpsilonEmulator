using Epsilon.Protocol;

namespace Epsilon.Gateway;

public sealed record ProtocolHealthSnapshot(
    ProtocolHealthState State,
    DateTime CheckedAtUtc,
    DateTime StartedAtUtc,
    ProtocolSelfCheckReport SelfCheck,
    int RecentPacketCount,
    int RecentServerErrorCount,
    DateTime? LastPacketAtUtc,
    IReadOnlyList<string> Alerts,
    IReadOnlyList<ProtocolHealthAlertRecord> AlertHistory);
