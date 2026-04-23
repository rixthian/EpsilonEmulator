using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.Protocol;

/// <summary>
/// Service registration helpers for protocol infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers packet manifests, command manifests, registries, and self-check services.
    /// </summary>
    public static IServiceCollection AddProtocolServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PacketManifestOptions>()
            .Bind(configuration.GetSection(PacketManifestOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Family), "Protocol family is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ManifestPath), "Protocol packet manifest path is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.CommandManifestPath), "Protocol command manifest path is required.")
            .ValidateOnStart();

        services.AddSingleton<PacketManifestLoader>();
        services.AddSingleton<PacketRegistry>();
        services.AddSingleton<ProtocolCommandManifestLoader>();
        services.AddSingleton<ProtocolCommandRegistry>();
        services.AddSingleton<ProtocolSelfCheckService>();
        return services;
    }
}
