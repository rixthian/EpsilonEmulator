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
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryCharacterProfileRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresCharacterProfileRepository>()));
        services.AddSingleton<ISubscriptionRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemorySubscriptionRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresSubscriptionRepository>()));
        services.AddSingleton<IPetProfileRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryPetProfileRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresPetProfileRepository>()));
        services.AddSingleton<IRoomRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomRepository>()));
        services.AddSingleton<IRoomLayoutRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomLayoutRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomLayoutRepository>()));
        services.AddSingleton<IRoomItemRepository>(provider =>
            ResolveProvider(
                provider,
                inMemory: sp => sp.GetRequiredService<InMemoryRoomItemRepository>(),
                postgres: sp => sp.GetRequiredService<PostgresRoomItemRepository>()));
        services.AddSingleton<IItemDefinitionRepository>(provider =>
            ResolveProvider(
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

        services.AddSingleton<InMemoryCharacterProfileRepository>();
        services.AddSingleton<InMemorySubscriptionRepository>();
        services.AddSingleton<InMemoryPetProfileRepository>();
        services.AddSingleton<InMemoryRoomRepository>();
        services.AddSingleton<InMemoryRoomLayoutRepository>();
        services.AddSingleton<InMemoryRoomItemRepository>();
        services.AddSingleton<InMemoryItemDefinitionRepository>();
        services.AddSingleton<InMemoryNavigatorPublicRoomRepository>();
        services.AddSingleton<InMemoryPublicRoomAssetPackageRepository>();
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
