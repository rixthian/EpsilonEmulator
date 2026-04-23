using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class CollectibleLoopsTests
{
    [Fact]
    public async Task GrantXp_LevelsCollectorUp()
    {
        ServiceProvider services = BuildServices();
        ICollectFeatService featureService = services.GetRequiredService<ICollectFeatService>();

        CollectorProgressSnapshot progress = await featureService.GrantXpAsync(
            new CharacterId(7),
            25,
            "test",
            TestContext.Current.CancellationToken);

        Assert.Equal(115, progress.Xp);
        Assert.Equal(2, progress.Level);
        Assert.Equal("silver", progress.MonthlyRewardTier);
    }

    [Fact]
    public async Task AccrueEmeralds_GrantsDailyBalance()
    {
        ServiceProvider services = BuildServices();
        ICollectFeatService featureService = services.GetRequiredService<ICollectFeatService>();

        WalletSnapshot? wallet = await featureService.AccrueEmeraldsAsync(
            new CharacterId(5),
            TestContext.Current.CancellationToken);

        Assert.NotNull(wallet);
        Assert.Equal(
            535,
            wallet!.Balances.First(balance => string.Equals(balance.CurrencyCode, "emeralds", StringComparison.OrdinalIgnoreCase)).Amount);
    }

    [Fact]
    public async Task OpenGiftBox_GrantsRewards()
    {
        ServiceProvider services = BuildServices();
        ICollectFeatService featureService = services.GetRequiredService<ICollectFeatService>();

        GiftBoxOpenResult? result = await featureService.OpenGiftBoxAsync(
            new CharacterId(7),
            "starter_crate",
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("collector_gift_lamp", result!.GrantedCollectibleKeys);
        Assert.Equal(50, result.GrantedEmeralds);
    }

    [Fact]
    public async Task ClaimFactory_ProducesReward()
    {
        ServiceProvider services = BuildServices();
        ICollectFeatService featureService = services.GetRequiredService<ICollectFeatService>();

        GiftBoxOpenResult? result = await featureService.ClaimFactoryAsync(
            new CharacterId(1),
            "furni_factory",
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("factory_furni_crate", result!.GrantedCollectibleKeys);
    }

    [Fact]
    public async Task RecycleAndMarketplace_OperateOnOwnership()
    {
        ServiceProvider services = BuildServices();
        ICollectFeatService featureService = services.GetRequiredService<ICollectFeatService>();
        ICollectibleOwnershipRepository ownershipRepository = services.GetRequiredService<ICollectibleOwnershipRepository>();
        IWalletRepository walletRepository = services.GetRequiredService<IWalletRepository>();

        GiftBoxOpenResult? recycle = await featureService.RecycleAsync(
            new CharacterId(1),
            "collectimatic_lamp",
            TestContext.Current.CancellationToken);
        Assert.NotNull(recycle);
        Assert.Contains("collectimatic_prize_lamp", recycle!.GrantedCollectibleKeys);

        MarketListingSnapshot? listing = await featureService.CreateListingAsync(
            new CharacterId(1),
            "collectimatic_prize_lamp",
            120,
            TestContext.Current.CancellationToken);
        Assert.NotNull(listing);

        MarketListingSnapshot? purchased = await featureService.BuyListingAsync(
            new CharacterId(5),
            listing!.ListingId,
            TestContext.Current.CancellationToken);
        Assert.NotNull(purchased);
        Assert.False(purchased!.IsActive);

        CollectibleOwnershipSnapshot? buyerOwnership = await ownershipRepository.GetByCharacterIdAsync(
            new CharacterId(5),
            TestContext.Current.CancellationToken);
        WalletSnapshot? sellerWallet = await walletRepository.GetByCharacterIdAsync(
            new CharacterId(1),
            TestContext.Current.CancellationToken);

        Assert.NotNull(buyerOwnership);
        Assert.Contains("collectimatic_prize_lamp", buyerOwnership!.OwnedCollectibleKeys);
        Assert.Equal(
            1920,
            sellerWallet!.Balances.First(balance => string.Equals(balance.CurrencyCode, "emeralds", StringComparison.OrdinalIgnoreCase)).Amount);
    }

    private static ServiceProvider BuildServices()
    {
        ConfigurationManager configuration = new();
        configuration["Infrastructure:Provider"] = "InMemory";

        ServiceCollection services = new();
        services.AddPersistenceRuntime(configuration);
        services.AddGameRuntime();
        services.AddCoreGameRuntime();
        return services.BuildServiceProvider();
    }
}
