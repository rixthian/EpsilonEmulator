using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.Games;

public static class DependencyInjection
{
    public static IServiceCollection AddGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IGameRuntimeService, GameRuntimeService>();
        services.AddSingleton<IBattleBallLifecycleService, BattleBallLifecycleService>();
        services.AddSingleton<ISnowStormLifecycleService, SnowStormLifecycleService>();
        services.AddSingleton<IWobbleSquabbleLifecycleService, WobbleSquabbleLifecycleService>();
        return services;
    }
}
