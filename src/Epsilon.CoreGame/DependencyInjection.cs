using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.CoreGame;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IHotelReadService, HotelReadService>();
        services.AddSingleton<IHotelBootstrapService, HotelBootstrapService>();
        return services;
    }
}
