using Epsilon.Persistence;
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
            .Validate(options => !string.IsNullOrWhiteSpace(options.PostgresConnectionString), "PostgreSQL connection string is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RedisConnectionString), "Redis connection string is required.")
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
            IPersistenceReadinessChecker persistenceChecker) =>
        {
            GatewayRuntimeOptions gateway = gatewayOptions.Value;
            InfrastructureOptions infrastructure = infrastructureOptions.Value;
            PersistenceReadinessReport readiness = persistenceChecker.Check();

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
                    outgoingPacketCount = packetRegistry.Outgoing.Count
                }
            });
        });

        return diagnostics;
    }
}
