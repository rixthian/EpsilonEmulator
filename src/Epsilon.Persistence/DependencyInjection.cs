using Epsilon.Content;
using Epsilon.CoreGame;
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
            .Validate(options => !string.IsNullOrWhiteSpace(options.RedisConnectionString), "Redis connection string is required.")
            .ValidateOnStart();

        services.AddSingleton(_ => InMemoryHotelSeedBuilder.Build());
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
        services.AddSingleton<INavigatorPublicRoomRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryNavigatorPublicRoomRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed navigator public-room repository is not wired yet.")));
        services.AddSingleton<IPublicRoomAssetPackageRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryPublicRoomAssetPackageRepository>(),
                postgres: sp => throw new NotSupportedException("Postgres-backed public-room asset package repository is not wired yet.")));
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

        services.AddSingleton<InMemoryCharacterProfileRepository>();
        services.AddSingleton<InMemorySubscriptionRepository>();
        services.AddSingleton<InMemoryPetProfileRepository>();
        services.AddSingleton<InMemoryWalletRepository>();
        services.AddSingleton<InMemoryMessengerRepository>();
        services.AddSingleton<InMemoryBadgeRepository>();
        services.AddSingleton<InMemoryAchievementRepository>();
        services.AddSingleton<InMemoryChatCommandRepository>();
        services.AddSingleton<InMemoryRoleAccessRepository>();
        services.AddSingleton<InMemoryHotelAdvertisementRepository>();
        services.AddSingleton<InMemorySupportCenterRepository>();
        services.AddSingleton<InMemoryRoomRepository>();
        services.AddSingleton<InMemoryRoomLayoutRepository>();
        services.AddSingleton<InMemoryRoomItemRepository>();
        services.AddSingleton<InMemoryItemDefinitionRepository>();
        services.AddSingleton<InMemoryNavigatorPublicRoomRepository>();
        services.AddSingleton<InMemoryPublicRoomAssetPackageRepository>();
        services.AddSingleton<InMemoryClientPackageRepository>();
        services.AddSingleton<InMemoryRoomRuntimeRepository>();
        services.AddSingleton<PostgresCharacterProfileRepository>();
        services.AddSingleton<PostgresSubscriptionRepository>();
        services.AddSingleton<PostgresPetProfileRepository>();
        services.AddSingleton<PostgresRoomRepository>();
        services.AddSingleton<PostgresRoomLayoutRepository>();
        services.AddSingleton<PostgresRoomItemRepository>();
        services.AddSingleton<PostgresItemDefinitionRepository>();
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
