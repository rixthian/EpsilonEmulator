using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.CoreGame;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IHotelOperationalState, HotelOperationalState>();
        services.AddSingleton<IAccessControlService, AccessControlService>();
        services.AddSingleton<IBadgeCatalogService, BadgeCatalogService>();
        services.AddSingleton<IGroupService, GroupService>();
        services.AddSingleton<IHotelReadService, HotelReadService>();
        services.AddSingleton<IHotelBootstrapService, HotelBootstrapService>();
        services.AddSingleton<IHotelSessionSnapshotService, HotelSessionSnapshotService>();
        services.AddSingleton<IHotelNavigatorService, HotelNavigatorService>();
        services.AddSingleton<IHousekeepingSnapshotService, HousekeepingSnapshotService>();
        services.AddSingleton<ISupportCenterService, SupportCenterService>();
        services.AddSingleton<IHotelCommerceService, HotelCommerceService>();
        services.AddSingleton<IHotelCommerceFeatureService, HotelCommerceFeatureService>();
        services.AddSingleton<IHotelPresentationService, HotelPresentationService>();
        services.AddSingleton<IHotelWorldFeatureService, HotelWorldFeatureService>();
        services.AddSingleton<IInterfacePreferenceService, InterfacePreferenceService>();
        services.AddSingleton<IRoomBotRuntimeService, RoomBotRuntimeService>();
        services.AddSingleton<IRoomEntryService, RoomEntryService>();
        services.AddSingleton<IRoomInteractionService, RoomInteractionService>();
        services.AddSingleton<IRoomRuntimeSnapshotService, RoomRuntimeSnapshotService>();
        return services;
    }
}
