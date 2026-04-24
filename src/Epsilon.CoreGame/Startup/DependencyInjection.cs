using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.CoreGame;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IHotelEventBus, InMemoryHotelEventBus>();
        services.AddSingleton<IHotelOperationalState, HotelOperationalState>();
        services.AddSingleton<IAccessControlService, AccessControlService>();
        services.AddSingleton<IBadgeCatalogService, BadgeCatalogService>();
        services.AddSingleton<IGroupService, GroupService>();
        services.AddSingleton<IStudioService, StudioService>();
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
        services.AddSingleton<IWalletChallengeService, WalletChallengeService>();
        services.AddSingleton<ICollectorProfileService, CollectorProfileService>();
        services.AddSingleton<ILaunchEntitlementService, LaunchEntitlementService>();
        services.AddSingleton<IEmeraldLedgerService, EmeraldLedgerService>();
        services.AddSingleton<ICollectFeatService, CollectFeatService>();
        services.AddSingleton<IRoomBotRuntimeService, RoomBotRuntimeService>();
        services.AddSingleton<IRoomAnimService, RoomAnimService>();
        services.AddSingleton<IRoomEntryService, RoomEntryService>();
        services.AddSingleton<IRoomInteractionService, RoomInteractionService>();
        services.AddSingleton<IRoomRollerService, RoomRollerService>();
        services.AddSingleton<IRoomTickScheduler, RoomTickScheduler>();
        services.AddSingleton<IRoomRuntimeSnapshotService, RoomRuntimeSnapshotService>();
        return services;
    }
}
