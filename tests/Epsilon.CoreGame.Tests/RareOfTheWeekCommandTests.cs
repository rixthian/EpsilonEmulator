using Epsilon.Content;
using Epsilon.Games;
using Epsilon.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Epsilon.CoreGame.Tests;

public sealed class RareOfTheWeekCommandTests
{
    [Fact]
    public async Task RareWeekSet_UpdatesCatalogLanding()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelPresentationService hotelPresentationService = services.GetRequiredService<IHotelPresentationService>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":rareweek set 2"),
            cancellationToken);

        Assert.True(result.Succeeded);

        CatalogLandingSnapshot landing = await hotelPresentationService.GetCatalogLandingAsync(new CharacterId(1), cancellationToken);
        Assert.NotNull(landing.RareOfTheWeek);
        Assert.Equal(2L, landing.RareOfTheWeek!.Offer.CatalogOfferId.Value);
    }

    [Fact]
    public async Task RareWeekOff_RemovesCatalogLandingHighlight()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IRoomInteractionService roomInteractionService = services.GetRequiredService<IRoomInteractionService>();
        IHotelPresentationService hotelPresentationService = services.GetRequiredService<IHotelPresentationService>();

        RoomChatResult result = await roomInteractionService.SendChatAsync(
            new RoomChatRequest(new CharacterId(1), new RoomId(1), ":rareweek off"),
            cancellationToken);

        Assert.True(result.Succeeded);

        CatalogLandingSnapshot landing = await hotelPresentationService.GetCatalogLandingAsync(new CharacterId(1), cancellationToken);
        Assert.Null(landing.RareOfTheWeek);
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

public sealed class InterfacePreferenceServiceTests
{
    [Fact]
    public async Task SetLanguage_ChangesCharacterInterfacePreference()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ServiceProvider services = BuildServices();
        IInterfacePreferenceService interfacePreferenceService = services.GetRequiredService<IInterfacePreferenceService>();

        InterfacePreferenceSnapshot updated = await interfacePreferenceService.SetLanguageAsync(
            new CharacterId(1),
            "es",
            cancellationToken);

        Assert.Equal("es", updated.SelectedLanguageCode);
        Assert.Contains(updated.SupportedLanguages, language => language.LanguageCode == "it");
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
