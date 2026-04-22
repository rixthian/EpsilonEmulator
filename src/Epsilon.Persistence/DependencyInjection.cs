using Epsilon.Auth;
using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Rooms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Epsilon.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Persistence provider is required.")
            .Validate(
                options => options.Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase) ||
                           options.Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase),
                "Persistence provider must be either InMemory or Postgres.")
            .Validate(
                options => !options.Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
                           !string.IsNullOrWhiteSpace(options.PostgresConnectionString),
                "PostgreSQL connection string is required when Infrastructure.Provider is Postgres.")
            .ValidateOnStart();

        services.AddSingleton(_ => InMemoryHotelSeedBuilder.Build());
        services.AddSingleton<IPacketLogger, InMemoryPacketLogger>();
        services.AddSingleton<IDevLoginCharacterResolver, DevLoginCharacterResolver>();
        services.AddSingleton<PostgresDataSourceProvider>();
        services.AddSingleton<ICharacterProfileRepository>(provider =>
            ResolveProvider<ICharacterProfileRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCharacterProfileRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresCharacterProfileRepository>()));
        services.AddSingleton<ISubscriptionRepository>(provider =>
            ResolveProvider<ISubscriptionRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemorySubscriptionRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresSubscriptionRepository>()));
        services.AddSingleton<IPetProfileRepository>(provider =>
            ResolveProvider<IPetProfileRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryPetProfileRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresPetProfileRepository>()));
        services.AddSingleton<IWalletRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryWalletRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed wallet repository is not wired yet.")));
        services.AddSingleton<ICharacterPreferenceRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCharacterPreferenceRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed character-interface-preference repository is not wired yet.")));
        services.AddSingleton<IInventoryRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryInventoryRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed inventory repository is not wired yet.")));
        services.AddSingleton<IMessengerRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryMessengerRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed messenger repository is not wired yet.")));
        services.AddSingleton<IBadgeRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryBadgeRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed badge repository is not wired yet.")));
        services.AddSingleton<IBadgeDefinitionRepository>(provider =>
            ResolveProvider<IBadgeDefinitionRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryBadgeDefinitionRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresBadgeDefinitionRepository>()));
        services.AddSingleton<IAchievementRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryAchievementRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed achievement repository is not wired yet.")));
        services.AddSingleton<IChatCommandRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryChatCommandRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed chat-command repository is not wired yet.")));
        services.AddSingleton<IRoleAccessRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoleAccessRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed role-access repository is not wired yet.")));
        services.AddSingleton<IHotelAdvertisementRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryHotelAdvertisementRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed advertisement repository is not wired yet.")));
        services.AddSingleton<ISupportCenterRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemorySupportCenterRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed support-center repository is not wired yet.")));
        services.AddSingleton<IModerationRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryModerationRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed moderation repository is not wired yet.")));
        services.AddSingleton<IRoomRepository>(provider =>
            ResolveProvider<IRoomRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomRepository>()));
        services.AddSingleton<IRoomLayoutRepository>(provider =>
            ResolveProvider<IRoomLayoutRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomLayoutRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomLayoutRepository>()));
        services.AddSingleton<IRoomItemRepository>(provider =>
            ResolveProvider<IRoomItemRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomItemRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomItemRepository>()));
        services.AddSingleton<IItemDefinitionRepository>(provider =>
            ResolveProvider<IItemDefinitionRepository>(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryItemDefinitionRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresItemDefinitionRepository>()));
        services.AddSingleton<ICatalogPageRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCatalogPageRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed catalog-page repository is not wired yet.")));
        services.AddSingleton<ICatalogOfferRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCatalogOfferRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed catalog-offer repository is not wired yet.")));
        services.AddSingleton<ICatalogCampaignRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCatalogCampaignRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed catalog-campaign repository is not wired yet.")));
        services.AddSingleton<ICatalogFeatureStateRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCatalogFeatureStateRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed catalog-feature-state repository is not wired yet.")));
        services.AddSingleton<IInterfaceLanguageRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryInterfaceLanguageRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed interface-language repository is not wired yet.")));
        services.AddSingleton<IEffectDefinitionRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryEffectDefinitionRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed effect-definition repository is not wired yet.")));
        services.AddSingleton<IRoomVisualSceneRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomVisualSceneRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed room-visual repository is not wired yet.")));
        services.AddSingleton<INavigatorPublicRoomRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryNavigatorPublicRoomRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed navigator public-room repository is not wired yet.")));
        services.AddSingleton<IPublicRoomBehaviorRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryPublicRoomBehaviorRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed public-room behavior repository is not wired yet.")));
        services.AddSingleton<IPublicRoomPackageRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryPublicRoomPackageRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed public-room asset package repository is not wired yet.")));
        services.AddSingleton<IGameDefinitionRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryGameDefinitionRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed game-definition repository is not wired yet.")));
        services.AddSingleton<IGameVenueRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryGameVenueRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed game-venue repository is not wired yet.")));
        services.AddSingleton<IGameSessionRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryGameSessionRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed game-session repository is not wired yet.")));
        services.AddSingleton<IVoucherRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryVoucherRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed voucher-definition repository is not wired yet.")));
        services.AddSingleton<IVoucherRedemptionRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryVoucherRedemptionRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed voucher-redemption repository is not wired yet.")));
        services.AddSingleton<ICollectibleRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCollectibleRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed collectible-definition repository is not wired yet.")));
        services.AddSingleton<IEcotronRewardRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryEcotronRewardRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed ecotron-reward repository is not wired yet.")));
        services.AddSingleton<IClientPackageRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryClientPackageRepository>(),
                postgres: sp => throw new NotSupportedException("Document-backed client-package repository is not wired yet.")));
        services.AddSingleton<IRoomRuntimeRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomRuntimeRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed room-runtime repository is not wired yet.")));

        services.AddSingleton<IAccountRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => (IAccountRepository)sp.GetRequiredService<InMemoryAccountRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed account repository is not wired yet.")));
        services.AddSingleton<IRegistrationService, RegistrationService>();
        services.AddSingleton<InMemoryAccountRepository>();
        services.AddSingleton<InMemoryCharacterProfileRepository>();
        services.AddSingleton<InMemorySubscriptionRepository>();
        services.AddSingleton<InMemoryPetProfileRepository>();
        services.AddSingleton<InMemoryWalletRepository>();
        services.AddSingleton<InMemoryCharacterPreferenceRepository>();
        services.AddSingleton<InMemoryInventoryRepository>();
        services.AddSingleton<InMemoryMessengerRepository>();
        services.AddSingleton<InMemoryBadgeRepository>();
        services.AddSingleton<InMemoryBadgeDefinitionRepository>();
        services.AddSingleton<InMemoryAchievementRepository>();
        services.AddSingleton<InMemoryChatCommandRepository>();
        services.AddSingleton<InMemoryRoleAccessRepository>();
        services.AddSingleton<InMemoryHotelAdvertisementRepository>();
        services.AddSingleton<InMemorySupportCenterRepository>();
        services.AddSingleton<InMemoryModerationRepository>();
        services.AddSingleton<InMemoryRoomRepository>();
        services.AddSingleton<InMemoryRoomLayoutRepository>();
        services.AddSingleton<InMemoryRoomItemRepository>();
        services.AddSingleton<InMemoryItemDefinitionRepository>();
        services.AddSingleton<InMemoryCatalogPageRepository>();
        services.AddSingleton<InMemoryCatalogOfferRepository>();
        services.AddSingleton<InMemoryCatalogCampaignRepository>();
        services.AddSingleton<InMemoryCatalogFeatureStateRepository>();
        services.AddSingleton<InMemoryInterfaceLanguageRepository>();
        services.AddSingleton<InMemoryEffectDefinitionRepository>();
        services.AddSingleton<InMemoryRoomVisualSceneRepository>();
        services.AddSingleton<InMemoryNavigatorPublicRoomRepository>();
        services.AddSingleton<InMemoryPublicRoomBehaviorRepository>();
        services.AddSingleton<InMemoryPublicRoomPackageRepository>();
        services.AddSingleton<InMemoryGameDefinitionRepository>();
        services.AddSingleton<InMemoryGameVenueRepository>();
        services.AddSingleton<InMemoryGameSessionRepository>();
        services.AddSingleton<InMemoryVoucherRepository>();
        services.AddSingleton<InMemoryVoucherRedemptionRepository>();
        services.AddSingleton<InMemoryCollectibleRepository>();
        services.AddSingleton<InMemoryEcotronRewardRepository>();
        services.AddSingleton<InMemoryClientPackageRepository>();
        services.AddSingleton<InMemoryRoomRuntimeRepository>();
        services.AddSingleton<LocalRoomRuntimeCoordinator>();
        services.AddSingleton<RedisRoomRuntimeCoordinator>();
        services.AddSingleton<IRoomRuntimeCoordinator>(provider =>
        {
            PersistenceOptions options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.RedisConnectionString))
            {
                return provider.GetRequiredService<RedisRoomRuntimeCoordinator>();
            }

            return provider.GetRequiredService<LocalRoomRuntimeCoordinator>();
        });
        services.AddSingleton<PostgresCharacterProfileRepository>();
        services.AddSingleton<PostgresSubscriptionRepository>();
        services.AddSingleton<PostgresPetProfileRepository>();
        services.AddSingleton<PostgresRoomRepository>();
        services.AddSingleton<PostgresRoomLayoutRepository>();
        services.AddSingleton<PostgresRoomItemRepository>();
        services.AddSingleton<PostgresItemDefinitionRepository>();
        services.AddSingleton<PostgresBadgeDefinitionRepository>();
        services.AddSingleton<IPersistenceReadinessChecker, PersistenceReadinessChecker>();
        return services;
    }

    private static T ResolveProvider<T>(
        IServiceProvider services,
        Func<IServiceProvider, T> inMemory,
        Func<IServiceProvider, T> postgres)
        where T : class
    {
        PersistenceOptions options = services.GetRequiredService<IOptions<PersistenceOptions>>().Value;

        if (options.Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return inMemory(services);
        }

        if (options.Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            return postgres(services);
        }

        throw new NotSupportedException($"Persistence provider '{options.Provider}' is not supported.");
    }
}
