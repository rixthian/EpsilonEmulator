using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.CoreGame;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IHotelReadService, HotelReadService>();
        services.AddSingleton<IHotelBootstrapService, HotelBootstrapService>();
        services.AddSingleton<IHotelSessionSnapshotService, HotelSessionSnapshotService>();
        services.AddSingleton<IHousekeepingSnapshotService, HousekeepingSnapshotService>();
        services.AddSingleton<ISupportCenterService, SupportCenterService>();
        services.AddSingleton<IRoomEntryService, RoomEntryService>();
        services.AddSingleton<IRoomInteractionService, RoomInteractionService>();
        services.AddSingleton<IRoomRuntimeSnapshotService, RoomRuntimeSnapshotService>();
        return services;
    }
}
