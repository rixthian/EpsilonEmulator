namespace Epsilon.Gateway;

public sealed record HotelIntelligenceScorecard(
    int Foundation,
    int Runtime,
    int Features,
    int ControlPlane,
    int Total);

public sealed record HotelIntelligenceSignals(
    int IncomingPacketCount,
    int OutgoingPacketCount,
    int RegisteredCommandCount,
    int ActiveRoomCount,
    int ActiveGameSessionCount,
    int GameDefinitionCount,
    int GameVenueCount,
    int VoucherCount,
    int CollectibleCount,
    int EcotronRewardCount,
    int RecentEventCount,
    IReadOnlyDictionary<string, int> RecentEventKinds);

public sealed record HotelIntelligenceRecommendation(
    string Severity,
    string Area,
    string Title,
    string Detail,
    string ActionKey);

public sealed record HotelIntelligenceSummary(
    DateTime GeneratedAtUtc,
    GatewayDiagnosticsSummary Gateway,
    HotelIntelligenceScorecard Scorecard,
    HotelIntelligenceSignals Signals,
    IReadOnlyList<HotelIntelligenceRecommendation> Recommendations);
