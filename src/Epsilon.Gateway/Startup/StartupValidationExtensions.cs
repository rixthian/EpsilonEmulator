using Epsilon.CoreGame;
using Epsilon.Persistence;
using Epsilon.Protocol;
using Microsoft.Extensions.Options;

namespace Epsilon.Gateway;

public static class StartupValidationExtensions
{
    public static IServiceCollection AddGatewayRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GatewayRuntimeOptions>()
            .Bind(configuration.GetSection(GatewayRuntimeOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.HotelName), "Gateway hotel name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.PublicHost), "Gateway public host is required.")
            .Validate(options => options.TcpPort is > 0 and <= 65535, "Gateway TCP port must be between 1 and 65535.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RealtimePath), "Gateway realtime path is required.")
            .Validate(options => options.RealtimePath.StartsWith('/'), "Gateway realtime path must start with '/'.")
            .Validate(options => options.RealtimeKeepAliveSeconds is > 0 and <= 300, "Gateway realtime keepalive must be between 1 and 300 seconds.")
            .ValidateOnStart();

        services.AddOptions<RoomTickSchedulerOptions>()
            .Bind(configuration.GetSection(RoomTickSchedulerOptions.SectionName))
            .Validate(options => options.TickIntervalMilliseconds is >= 50 and <= 5000, "Room tick interval must be between 50 and 5000 milliseconds.")
            .Validate(options => options.RollerIntervalTicks is > 0 and <= 60, "Room roller interval must be between 1 and 60 ticks.")
            .ValidateOnStart();

        services.AddOptions<ProtocolHealthMonitorOptions>()
            .Bind(configuration.GetSection(ProtocolHealthMonitorOptions.SectionName))
            .Validate(options => options.CheckIntervalSeconds is > 0 and <= 3600, "Protocol health check interval must be between 1 and 3600 seconds.")
            .Validate(options => options.RecentPacketWindowSeconds is > 0 and <= 3600, "Protocol health packet window must be between 1 and 3600 seconds.")
            .Validate(options => options.IdleWarningSeconds is > 0 and <= 86400, "Protocol health idle warning must be between 1 and 86400 seconds.")
            .Validate(options => options.IdleCriticalSeconds >= options.IdleWarningSeconds, "Protocol health idle critical threshold must be greater than or equal to the warning threshold.")
            .Validate(options => options.RealtimeIdleWarningSeconds is > 0 and <= 86400, "Protocol realtime idle warning must be between 1 and 86400 seconds.")
            .Validate(options => options.RealtimeIdleCriticalSeconds >= options.RealtimeIdleWarningSeconds, "Protocol realtime idle critical threshold must be greater than or equal to the warning threshold.")
            .ValidateOnStart();

        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Infrastructure provider is required.")
            .Validate(
                options => !string.Equals(options.Provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                           !string.IsNullOrWhiteSpace(options.PostgresConnectionString),
                "PostgreSQL connection string is required when Infrastructure.Provider is Postgres.")
            .ValidateOnStart();

        services.AddSingleton<ProtocolCommandExecutionService>();
        services.AddSingleton<HotelRealtimeSocketHandler>();
        services.AddSingleton<IRealtimeConnectionMonitor, RealtimeConnectionMonitor>();
        // HOTFIX broadcast: singleton hub for real-time room peer notifications.
        services.AddSingleton<IRoomConnectionHub, RoomConnectionHub>();
        services.AddSingleton<IProtocolHealthMonitor, ProtocolHealthMonitor>();
        services.AddSingleton<IHotelIntelligenceService, HotelIntelligenceService>();
        services.AddHostedService<ProtocolHealthMonitorWorker>();
        services.AddHostedService<RoomTickWorker>();

        return services;
    }

    public static RouteGroupBuilder MapRuntimeDiagnostics(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder diagnostics = app.MapGroup("/diagnostics");

        diagnostics.MapGet("/configuration", (
            IOptions<GatewayRuntimeOptions> gatewayOptions,
            IOptions<InfrastructureOptions> infrastructureOptions,
            PacketRegistry packetRegistry,
            ProtocolCommandRegistry commandRegistry,
            ProtocolSelfCheckService protocolSelfCheckService,
            IPersistenceReadinessChecker persistenceChecker) =>
        {
            GatewayRuntimeOptions gateway = gatewayOptions.Value;
            InfrastructureOptions infrastructure = infrastructureOptions.Value;
            PersistenceReadinessReport readiness = persistenceChecker.Check();
            ProtocolSelfCheckReport protocolSelfCheck = protocolSelfCheckService.Run();

            return Results.Ok(new
            {
                gateway = new
                {
                    gateway.HotelName,
                    gateway.PublicHost,
                    gateway.TcpPort,
                    gateway.RealtimePath,
                    gateway.RequireTlsForRealtime,
                    gateway.RealtimeKeepAliveSeconds
                },
                infrastructure = new
                {
                    postgresConfigured = !string.IsNullOrWhiteSpace(infrastructure.PostgresConnectionString),
                    redisConfigured = !string.IsNullOrWhiteSpace(infrastructure.RedisConnectionString)
                },
                readiness,
                protocol = new
                {
                    packetRegistry.Family,
                    incomingPacketCount = packetRegistry.Incoming.Count,
                    outgoingPacketCount = packetRegistry.Outgoing.Count,
                    commandFamily = commandRegistry.Family,
                    commandRevision = commandRegistry.Revision,
                    commandCount = commandRegistry.Commands.Count,
                    selfCheckHealthy = protocolSelfCheck.IsHealthy
                }
            });
        });

        diagnostics.MapGet("/summary", (
            IOptions<GatewayRuntimeOptions> gatewayOptions,
            IPersistenceReadinessChecker persistenceChecker,
            IProtocolHealthMonitor protocolHealthMonitor,
            IRealtimeConnectionMonitor realtimeConnectionMonitor) =>
        {
            GatewayRuntimeOptions gateway = gatewayOptions.Value;
            PersistenceReadinessReport readiness = persistenceChecker.Check();
            ProtocolHealthSnapshot protocol = protocolHealthMonitor.GetSnapshot();
            RealtimeConnectionSnapshot realtimeConnections = realtimeConnectionMonitor.GetSnapshot();

            GatewayDiagnosticsSummary summary = new(
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

            return Results.Ok(summary);
        });

        diagnostics.MapGet("/intelligence", async (
            IHotelIntelligenceService hotelIntelligenceService,
            CancellationToken cancellationToken) =>
        {
            HotelIntelligenceSummary summary = await hotelIntelligenceService.BuildAsync(cancellationToken);
            return Results.Ok(summary);
        });

        diagnostics.MapGet("/protocol", (
            PacketRegistry packetRegistry,
            ProtocolCommandRegistry commandRegistry,
            ProtocolSelfCheckService protocolSelfCheckService) =>
        {
            ProtocolSelfCheckReport report = protocolSelfCheckService.Run();

            return Results.Ok(new
            {
                report,
                incomingPackets = packetRegistry.Incoming,
                outgoingPackets = packetRegistry.Outgoing,
                commands = commandRegistry.Commands
            });
        });

        diagnostics.MapGet("/protocol/health", (
            IProtocolHealthMonitor protocolHealthMonitor) =>
        {
            return Results.Ok(protocolHealthMonitor.GetSnapshot());
        });

        diagnostics.MapGet("/packetlog", (
            IPacketLogger packetLogger,
            int count = 200) =>
        {
            IReadOnlyList<PacketLogEntry> entries = packetLogger.GetRecent(Math.Clamp(count, 1, 2000));
            return Results.Ok(new { count = entries.Count, entries });
        });

        return diagnostics;
    }
}
