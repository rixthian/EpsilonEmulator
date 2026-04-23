using Epsilon.Persistence;

namespace Epsilon.Gateway;

public sealed record GatewaySummaryIdentity(
    string HotelName,
    string PublicHost,
    int TcpPort,
    string RealtimePath,
    bool RequireTlsForRealtime,
    int RealtimeKeepAliveSeconds);

public sealed record GatewayRealtimeStatus(
    string Transport,
    string RealtimePath,
    bool TlsRequired,
    bool LoopbackInsecureAllowed,
    int KeepAliveSeconds,
    bool Ready,
    RealtimeConnectionSnapshot Connections);

public sealed record GatewayOverallStatus(
    bool Healthy,
    string Status);

public sealed record GatewayDiagnosticsSummary(
    GatewaySummaryIdentity Gateway,
    GatewayRealtimeStatus Realtime,
    PersistenceReadinessReport Persistence,
    ProtocolHealthSnapshot Protocol,
    GatewayOverallStatus Overall);
