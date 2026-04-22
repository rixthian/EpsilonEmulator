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
            .ValidateOnStart();

        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Infrastructure provider is required.")
            .Validate(
                options => !string.Equals(options.Provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                           !string.IsNullOrWhiteSpace(options.PostgresConnectionString),
                "PostgreSQL connection string is required when Infrastructure.Provider is Postgres.")
            .ValidateOnStart();

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
                    gateway.TcpPort
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
