using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Persistence;
using Epsilon.Protocol;
using Microsoft.Extensions.Options;

namespace Epsilon.Gateway;

public sealed class HotelIntelligenceService : IHotelIntelligenceService
{
    private readonly IOptions<GatewayRuntimeOptions> _gatewayOptions;
    private readonly IPersistenceReadinessChecker _persistenceReadinessChecker;
    private readonly IProtocolHealthMonitor _protocolHealthMonitor;
    private readonly IRealtimeConnectionMonitor _realtimeConnectionMonitor;
    private readonly IHotelWorldFeatureService _hotelWorldFeatureService;
    private readonly IHotelCommerceFeatureService _hotelCommerceFeatureService;
    private readonly IGameRuntimeService _gameRuntimeService;
    private readonly IRoomRuntimeRepository _roomRuntimeRepository;
    private readonly IHotelEventBus _hotelEventBus;
    private readonly PacketRegistry _packetRegistry;
    private readonly ProtocolCommandRegistry _commandRegistry;

    public HotelIntelligenceService(
        IOptions<GatewayRuntimeOptions> gatewayOptions,
        IPersistenceReadinessChecker persistenceReadinessChecker,
        IProtocolHealthMonitor protocolHealthMonitor,
        IRealtimeConnectionMonitor realtimeConnectionMonitor,
        IHotelWorldFeatureService hotelWorldFeatureService,
        IHotelCommerceFeatureService hotelCommerceFeatureService,
        IGameRuntimeService gameRuntimeService,
        IRoomRuntimeRepository roomRuntimeRepository,
        IHotelEventBus hotelEventBus,
        PacketRegistry packetRegistry,
        ProtocolCommandRegistry commandRegistry)
    {
        _gatewayOptions = gatewayOptions;
        _persistenceReadinessChecker = persistenceReadinessChecker;
        _protocolHealthMonitor = protocolHealthMonitor;
        _realtimeConnectionMonitor = realtimeConnectionMonitor;
        _hotelWorldFeatureService = hotelWorldFeatureService;
        _hotelCommerceFeatureService = hotelCommerceFeatureService;
        _gameRuntimeService = gameRuntimeService;
        _roomRuntimeRepository = roomRuntimeRepository;
        _hotelEventBus = hotelEventBus;
        _packetRegistry = packetRegistry;
        _commandRegistry = commandRegistry;
    }

    public async ValueTask<HotelIntelligenceSummary> BuildAsync(
        CancellationToken cancellationToken = default)
    {
        GatewayRuntimeOptions gateway = _gatewayOptions.Value;
        PersistenceReadinessReport readiness = _persistenceReadinessChecker.Check();
        ProtocolHealthSnapshot protocol = _protocolHealthMonitor.GetSnapshot();
        RealtimeConnectionSnapshot realtimeConnections = _realtimeConnectionMonitor.GetSnapshot();
        GameCatalogSnapshot gameCatalog = await _hotelWorldFeatureService.GetGameCatalogAsync(cancellationToken);
        HotelCommerceFeatureSnapshot commerce = await _hotelCommerceFeatureService.GetSnapshotAsync(cancellationToken);
        IReadOnlyList<GameSessionState> activeGameSessions = await _gameRuntimeService.GetActiveSessionsAsync(cancellationToken);
        IReadOnlyList<RoomId> activeRooms = await _roomRuntimeRepository.GetAllActiveRoomIdsAsync(cancellationToken);
        IReadOnlyList<HotelEventEnvelope> recentEvents = await _hotelEventBus.GetRecentAsync(64, cancellationToken);

        GatewayDiagnosticsSummary gatewaySummary = new(
            Gateway: new GatewaySummaryIdentity(
                gateway.HotelName,
                gateway.PublicHost,
                gateway.TcpPort,
                gateway.RealtimePath,
                gateway.RequireTlsForRealtime,
                gateway.RealtimeKeepAliveSeconds),
            Realtime: new GatewayRealtimeStatus(
                gateway.RequireTlsForRealtime ? "wss" : "ws",
                gateway.RealtimePath,
                gateway.RequireTlsForRealtime,
                gateway.AllowInsecureLoopbackRealtime,
                gateway.RealtimeKeepAliveSeconds,
                !string.IsNullOrWhiteSpace(gateway.RealtimePath),
                realtimeConnections),
            Persistence: readiness,
            Protocol: protocol,
            Overall: new GatewayOverallStatus(
                readiness.IsReady && protocol.State is ProtocolHealthState.Healthy,
                !readiness.IsReady || protocol.State is ProtocolHealthState.Critical
                    ? "critical"
                    : protocol.State is ProtocolHealthState.Warning
                        ? "warning"
                        : "healthy"));

        Dictionary<string, int> recentEventKinds = recentEvents
            .GroupBy(item => item.Kind.ToString(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        HotelIntelligenceSignals signals = new(
            IncomingPacketCount: _packetRegistry.Incoming.Count,
            OutgoingPacketCount: _packetRegistry.Outgoing.Count,
            RegisteredCommandCount: _commandRegistry.Commands.Count,
            ActiveRoomCount: activeRooms.Count,
            ActiveGameSessionCount: activeGameSessions.Count,
            GameDefinitionCount: gameCatalog.Games.Count,
            GameVenueCount: gameCatalog.Venues.Count,
            VoucherCount: commerce.Vouchers.Count,
            CollectibleCount: commerce.Collectibles.Count,
            EcotronRewardCount: commerce.EcotronRewards.Count,
            RecentEventCount: recentEvents.Count,
            RecentEventKinds: recentEventKinds);

        HotelIntelligenceScorecard scorecard = BuildScorecard(
            readiness,
            protocol,
            realtimeConnections,
            signals);

        HotelIntelligenceRecommendation[] recommendations = BuildRecommendations(
            readiness,
            protocol,
            realtimeConnections,
            signals);

        return new HotelIntelligenceSummary(
            GeneratedAtUtc: DateTime.UtcNow,
            Gateway: gatewaySummary,
            Scorecard: scorecard,
            Signals: signals,
            Recommendations: recommendations);
    }

    private static HotelIntelligenceScorecard BuildScorecard(
        PersistenceReadinessReport readiness,
        ProtocolHealthSnapshot protocol,
        RealtimeConnectionSnapshot realtimeConnections,
        HotelIntelligenceSignals signals)
    {
        int foundation = 0;
        foundation += readiness.IsReady ? 30 : 10;
        foundation += protocol.State switch
        {
            ProtocolHealthState.Healthy => 30,
            ProtocolHealthState.Warning => 18,
            ProtocolHealthState.Critical => 5,
            _ => 10
        };
        foundation += signals.RegisteredCommandCount >= 8 ? 20 : Math.Min(20, signals.RegisteredCommandCount * 2);
        foundation += signals.IncomingPacketCount > 0 && signals.OutgoingPacketCount > 0 ? 20 : 5;

        int runtime = 0;
        runtime += realtimeConnections.ActiveConnections > 0 ? 30 : 10;
        runtime += realtimeConnections.TotalAcceptedConnections > 0 ? 20 : 5;
        runtime += signals.ActiveRoomCount > 0 ? 25 : 10;
        runtime += signals.RecentEventCount >= 8 ? 25 : Math.Min(25, signals.RecentEventCount * 3);

        int features = 0;
        features += signals.GameDefinitionCount > 0 ? 20 : 0;
        features += signals.GameVenueCount > 0 ? 15 : 0;
        features += signals.ActiveGameSessionCount > 0 ? 10 : 0;
        features += signals.VoucherCount > 0 ? 10 : 0;
        features += signals.CollectibleCount > 0 ? 15 : 0;
        features += signals.EcotronRewardCount > 0 ? 10 : 0;
        features += signals.RecentEventKinds.ContainsKey(nameof(HotelEventKind.BotConfigurationChanged)) ? 10 : 0;
        features += signals.RecentEventKinds.ContainsKey(nameof(HotelEventKind.ChatMessagePublished)) ? 10 : 0;

        int controlPlane = 0;
        controlPlane += readiness.RedisConfigured ? 20 : 5;
        controlPlane += readiness.PostgresConfigured ? 20 : 5;
        controlPlane += protocol.AlertHistory.Count == 0 ? 20 : 10;
        controlPlane += signals.RecentEventKinds.ContainsKey(nameof(HotelEventKind.ModerationActionExecuted)) ? 20 : 10;
        controlPlane += signals.RecentEventKinds.ContainsKey(nameof(HotelEventKind.WalletAdjusted)) ? 20 : 10;

        foundation = Math.Min(100, foundation);
        runtime = Math.Min(100, runtime);
        features = Math.Min(100, features);
        controlPlane = Math.Min(100, controlPlane);

        int total = (int)Math.Round((foundation + runtime + features + controlPlane) / 4.0, MidpointRounding.AwayFromZero);
        return new HotelIntelligenceScorecard(foundation, runtime, features, controlPlane, total);
    }

    private static HotelIntelligenceRecommendation[] BuildRecommendations(
        PersistenceReadinessReport readiness,
        ProtocolHealthSnapshot protocol,
        RealtimeConnectionSnapshot realtimeConnections,
        HotelIntelligenceSignals signals)
    {
        List<HotelIntelligenceRecommendation> recommendations = [];

        if (!readiness.IsReady)
        {
            recommendations.Add(new(
                "critical",
                "persistence",
                "Persistence is not production-ready",
                string.Join("; ", readiness.Issues),
                "persistence.harden"));
        }

        if (protocol.State is ProtocolHealthState.Warning or ProtocolHealthState.Critical)
        {
            recommendations.Add(new(
                protocol.State is ProtocolHealthState.Critical ? "critical" : "warning",
                "protocol",
                "Protocol health is degraded",
                protocol.Alerts.Count == 0
                    ? "Protocol monitor reported degradation without explicit alert text."
                    : string.Join("; ", protocol.Alerts),
                "protocol.stabilize"));
        }

        if (realtimeConnections.TotalAcceptedConnections == 0)
        {
            recommendations.Add(new(
                "warning",
                "realtime",
                "Realtime plane has not been exercised",
                "WebSocket transport is configured, but no realtime connections have been accepted yet.",
                "realtime.exercise"));
        }

        if (signals.RecentEventCount == 0)
        {
            recommendations.Add(new(
                "warning",
                "events",
                "No hotel events have been observed",
                "The event bus is live, but no recent hotel activity has been published through it.",
                "events.activate"));
        }
        else if (!signals.RecentEventKinds.ContainsKey(nameof(HotelEventKind.RoomActorMoved)))
        {
            recommendations.Add(new(
                "info",
                "playability",
                "Movement events have not been observed",
                "Recent hotel activity did not include room movement. Tick scheduling and movement animation are still the next playability unlock.",
                "rooms.tick_scheduler"));
        }

        if (signals.GameDefinitionCount == 0 || signals.GameVenueCount == 0)
        {
            recommendations.Add(new(
                "warning",
                "games",
                "Game world catalog is incomplete",
                "Game definitions or venues are missing from the live catalog snapshot.",
                "games.catalog_seed"));
        }
        else if (signals.ActiveGameSessionCount == 0)
        {
            recommendations.Add(new(
                "info",
                "games",
                "No live game sessions are active",
                "Game catalog is present, but there are no active runtime sessions. Session orchestration and event projection should be exercised.",
                "games.runtime_exercise"));
        }

        if (signals.CollectibleCount == 0 && signals.VoucherCount == 0 && signals.EcotronRewardCount == 0)
        {
            recommendations.Add(new(
                "info",
                "commerce",
                "Commerce side systems are thin",
                "Collectibles, vouchers, and Ecotron rewards are all absent from the current live snapshot.",
                "commerce.feature_seed"));
        }

        return recommendations.ToArray();
    }
}
