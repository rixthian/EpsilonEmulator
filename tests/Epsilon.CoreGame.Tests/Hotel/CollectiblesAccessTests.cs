using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class CollectiblesAccessTests
{
    [Fact]
    public async Task WalletChallenge_VerifyAndLink_CreatesPrimaryLink()
    {
        ServiceProvider services = BuildServices();
        IWalletChallengeService challengeService = services.GetRequiredService<IWalletChallengeService>();
        IWalletLinkRepository walletLinkRepository = services.GetRequiredService<IWalletLinkRepository>();

        WalletChallengeSnapshot challenge = await challengeService.IssueAsync(
            new CharacterId(7),
            "0xVector000000000000000000000000000000000007",
            "metamask",
            TestContext.Current.CancellationToken);

        WalletLinkSnapshot? link = await challengeService.VerifyAndLinkAsync(
            new CharacterId(7),
            challenge.ChallengeId,
            $"devsig:{challenge.Nonce}",
            TestContext.Current.CancellationToken);

        IReadOnlyList<WalletLinkSnapshot> links = await walletLinkRepository.GetByCharacterIdAsync(
            new CharacterId(7),
            TestContext.Current.CancellationToken);

        Assert.NotNull(link);
        Assert.Single(links);
        Assert.True(links[0].IsPrimary);
        Assert.Equal("0xVector000000000000000000000000000000000007", links[0].WalletAddress);
    }

    [Fact]
    public async Task LaunchEntitlement_SeededCollectorPasses_AndUnlinkedPlayerFails()
    {
        ServiceProvider services = BuildServices();
        ILaunchEntitlementService launchEntitlementService = services.GetRequiredService<ILaunchEntitlementService>();

        LaunchEntitlementSnapshot seeded = await launchEntitlementService.EvaluateAsync(
            new CharacterId(1),
            TestContext.Current.CancellationToken);
        LaunchEntitlementSnapshot regular = await launchEntitlementService.EvaluateAsync(
            new CharacterId(7),
            TestContext.Current.CancellationToken);

        Assert.True(seeded.CanLaunch);
        Assert.False(regular.CanLaunch);
        Assert.Contains("wallet_link", regular.MissingRequirementKeys);
        Assert.Contains("premium_collectible", regular.MissingRequirementKeys);
    }

    [Fact]
    public async Task EmeraldLedger_GrantAndSpend_AdjustsWalletBalance()
    {
        ServiceProvider services = BuildServices();
        IEmeraldLedgerService emeraldLedgerService = services.GetRequiredService<IEmeraldLedgerService>();
        IWalletRepository walletRepository = services.GetRequiredService<IWalletRepository>();

        WalletSnapshot? granted = await emeraldLedgerService.GrantAsync(
            new CharacterId(7),
            250,
            "game_reward",
            TestContext.Current.CancellationToken);
        WalletSnapshot? spent = await emeraldLedgerService.SpendAsync(
            new CharacterId(7),
            100,
            "room_upgrade",
            TestContext.Current.CancellationToken);
        WalletSnapshot? finalWallet = await walletRepository.GetByCharacterIdAsync(
            new CharacterId(7),
            TestContext.Current.CancellationToken);

        Assert.NotNull(granted);
        Assert.NotNull(spent);
        Assert.NotNull(finalWallet);
        Assert.Equal(
            150,
            finalWallet!.Balances.First(balance => string.Equals(balance.CurrencyCode, "emeralds", StringComparison.OrdinalIgnoreCase)).Amount);
        Assert.Equal("room_upgrade", finalWallet.RecentEntries[0].ReasonCode);
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
