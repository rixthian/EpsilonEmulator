using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class ModerationCommandTests
{
    [Fact]
    public async Task Kick_RemovesTargetActorFromRoomRuntime()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IRoomRuntimeRepository roomRuntimeRepository = services.GetRequiredService<IRoomRuntimeRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(4), new RoomId(1), null, false), cancellationToken);

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(4), new RoomId(1), ":kick epsilon"),
            cancellationToken);

        RoomActorState? actor = await roomRuntimeRepository.GetActorByIdAsync(new RoomId(1), 1, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(actor);
    }

    [Fact]
    public async Task Shutup_BlocksTargetChatUntilUnmute()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(4), new RoomId(1), null, false), cancellationToken);

        RoomChatResult muteResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(4), new RoomId(1), ":shutup epsilon"),
            cancellationToken);
        RoomChatResult blockedChat = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), "can anyone hear me"),
            cancellationToken);
        RoomChatResult unmuteResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(4), new RoomId(1), ":unmute epsilon"),
            cancellationToken);

        Assert.True(muteResult.Succeeded);
        Assert.False(blockedChat.Succeeded);
        Assert.Contains("muted", blockedChat.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.True(unmuteResult.Succeeded);
    }

    [Fact]
    public async Task Ban_BlocksFutureRoomEntry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(4), new RoomId(1), null, false), cancellationToken);

        RoomChatResult banResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(4), new RoomId(1), ":ban epsilon 120 flooding"),
            cancellationToken);
        RoomEntryResult entryResult = await roomEntryService.EnterAsync(
            new RoomEntryRequest(new CharacterId(1), new RoomId(10), null, false),
            cancellationToken);

        Assert.True(banResult.Succeeded);
        Assert.False(entryResult.Succeeded);
        Assert.Equal(RoomEntryFailureCode.Banned, entryResult.FailureCode);
    }

    [Fact]
    public async Task Transfer_CreditsWalletForRoomPresentTarget()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomEntryService roomEntryService = services.GetRequiredService<IRoomEntryService>();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IWalletRepository walletRepository = services.GetRequiredService<IWalletRepository>();

        await roomEntryService.EnterAsync(new RoomEntryRequest(new CharacterId(4), new RoomId(1), null, false), cancellationToken);

        WalletSnapshot before = (await walletRepository.GetByCharacterIdAsync(new CharacterId(1), cancellationToken))!;

        RoomChatResult transferResult = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(4), new RoomId(1), ":transfer epsilon 50"),
            cancellationToken);

        WalletSnapshot after = (await walletRepository.GetByCharacterIdAsync(new CharacterId(1), cancellationToken))!;
        int beforeCredits = before.Balances.First(balance => balance.CurrencyCode == "credits").Amount;
        int afterCredits = after.Balances.First(balance => balance.CurrencyCode == "credits").Amount;

        Assert.True(transferResult.Succeeded);
        Assert.Equal(beforeCredits + 50, afterCredits);
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
