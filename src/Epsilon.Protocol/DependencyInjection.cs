using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.Protocol;

public static class DependencyInjection
{
    public static IServiceCollection AddProtocolServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PacketManifestOptions>(configuration.GetSection(PacketManifestOptions.SectionName));
        services.AddSingleton<PacketManifestLoader>();
        services.AddSingleton<PacketRegistry>();
        return services;
    }
}
