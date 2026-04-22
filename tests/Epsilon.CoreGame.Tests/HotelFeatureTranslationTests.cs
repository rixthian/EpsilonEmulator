using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class HotelFeatureTranslationTests
{
    [Fact]
    public async Task GameCatalog_IncludesClassicGameModulesAndVenues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelWorldFeatureService hotelWorldFeatureService = services.GetRequiredService<IHotelWorldFeatureService>();

        GameCatalogSnapshot snapshot = await hotelWorldFeatureService.GetGameCatalogAsync(cancellationToken);

        Assert.Contains(snapshot.Games, game => game.GameKey == "battleball");
        Assert.Contains(snapshot.Games, game => game.GameKey == "snowstorm");
        Assert.Contains(snapshot.Games, game => game.GameKey == "wobblesquabble");
        Assert.Contains(snapshot.Venues, venue => venue.AssetPackageKey == "battleball_stadium_core");
    }

    [Fact]
    public async Task PublicRoomBehaviors_ExposeClassicLidoBehaviorSurface()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelWorldFeatureService hotelWorldFeatureService = services.GetRequiredService<IHotelWorldFeatureService>();

        PublicRoomBehaviorSnapshot? snapshot =
            await hotelWorldFeatureService.GetPublicRoomBehaviorSnapshotAsync(2, cancellationToken);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot!.Behaviors, behavior => behavior.InteractionType == "swimming_pool");
        Assert.Contains(snapshot.Behaviors, behavior => behavior.InteractionType == "diving_deck");
    }

    [Fact]
    public async Task CommerceFeatures_ExposeVouchersCollectiblesAndEcotron()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelCommerceFeatureService hotelCommerceFeatureService = services.GetRequiredService<IHotelCommerceFeatureService>();

        HotelCommerceFeatureSnapshot snapshot = await hotelCommerceFeatureService.GetSnapshotAsync(cancellationToken);

        Assert.Contains(snapshot.Vouchers, voucher => voucher.Definition.VoucherCode == "WELCOME_CREDITS");
        Assert.Contains(snapshot.Collectibles, collectible => collectible.Definition.CategoryKey == "rare_of_week");
        Assert.Contains(snapshot.EcotronRewards, reward => reward.Definition.RewardKey == "ecotron_tier_1");
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";
        configuration["Infrastructure:RedisConnectionString"] = "localhost:6379";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
