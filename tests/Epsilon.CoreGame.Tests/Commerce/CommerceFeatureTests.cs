using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class CommerceFeatureTests
{
    [Fact]
    public async Task BundleOffer_ExpandsIntoInventoryItems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelCommerceService hotelCommerceService = services.GetRequiredService<IHotelCommerceService>();

        CatalogPurchaseResult result = await hotelCommerceService.PurchaseAsync(
            new CatalogPurchaseRequest(new CharacterId(1), new CatalogOfferId(5)),
            cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Inventory);
        Assert.True(result.Inventory!.Items.Count >= 5);
    }

    [Fact]
    public async Task VoucherRedemption_CreditsWalletAndBlocksSecondUse()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelCommerceService hotelCommerceService = services.GetRequiredService<IHotelCommerceService>();

        RedeemVoucherResult first = await hotelCommerceService.RedeemVoucherAsync(
            new RedeemVoucherRequest(new CharacterId(1), "WELCOME_CREDITS"),
            cancellationToken);
        RedeemVoucherResult second = await hotelCommerceService.RedeemVoucherAsync(
            new RedeemVoucherRequest(new CharacterId(1), "WELCOME_CREDITS"),
            cancellationToken);

        Assert.True(first.Succeeded);
        Assert.NotNull(first.Wallet);
        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task VoucherRedemption_RejectsUnknownCharacter()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelCommerceService hotelCommerceService = services.GetRequiredService<IHotelCommerceService>();

        RedeemVoucherResult result = await hotelCommerceService.RedeemVoucherAsync(
            new RedeemVoucherRequest(new CharacterId(9999), "WELCOME_CREDITS"),
            cancellationToken);

        Assert.False(result.Succeeded);
        Assert.Null(result.Wallet);
    }

    [Fact]
    public async Task NavigatorSearch_FiltersRecommendedClassicPublicRooms()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IHotelNavigatorService hotelNavigatorService = services.GetRequiredService<IHotelNavigatorService>();

        NavigatorSearchSnapshot snapshot = await hotelNavigatorService.SearchPublicRoomsAsync(
            new NavigatorSearchRequest("Lido", false),
            cancellationToken);

        Assert.Contains(snapshot.PublicRooms, room => room.Entry.Caption == "Lido Deck");

        NavigatorSearchSnapshot recommended = await hotelNavigatorService.SearchPublicRoomsAsync(
            new NavigatorSearchRequest(null, true),
            cancellationToken);

        Assert.Contains(recommended.PublicRooms, room => room.Entry.AssetPackageKey == "battleball_stadium_core");
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
