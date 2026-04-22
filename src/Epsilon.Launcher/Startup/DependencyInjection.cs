using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.Launcher;

public static class DependencyInjection
{
    public static IServiceCollection AddLauncherRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LauncherRuntimeOptions>()
            .Bind(configuration.GetSection(LauncherRuntimeOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceName), "Launcher service name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.GatewayBaseUrl), "Launcher gateway base URL is required.")
            .Validate(options => options.ClientProfiles.Count > 0, "At least one launcher client profile is required.")
            .Validate(options => options.ConnectionProfiles.Count > 0, "At least one launcher connection profile is required.")
            .ValidateOnStart();

        services.AddSingleton<ILauncherBootstrapService, LauncherBootstrapService>();
        return services;
    }
}
