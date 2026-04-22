using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Epsilon.Content;
using Epsilon.CoreGame;
using Epsilon.Games;
using Epsilon.Rooms;

namespace Epsilon.Persistence;

internal static class EmbeddedSeedContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static EmbeddedSeedContentLoader()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static IReadOnlyList<InterfaceLanguageDefinition> LoadInterfaceLanguages() =>
        Load<List<InterfaceLanguageDefinition>>("interface-languages.json");

    public static IReadOnlyList<ItemDefinition> LoadItemDefinitions()
    {
        IReadOnlyList<ItemDefinitionSeedModel> models = Load<List<ItemDefinitionSeedModel>>("item-definitions.json");
        return models.Select(model => new ItemDefinition(
            new ItemDefinitionId(model.ItemDefinitionId),
            model.PublicName,
            model.InternalName,
            model.ItemTypeCode,
            model.SpriteId,
            model.StackHeight,
            model.CanStack,
            model.CanSit,
            model.IsWalkable,
            model.AllowRecycle,
            model.AllowTrade,
            model.AllowMarketplaceSell,
            model.AllowGift,
            model.AllowInventoryStack,
            model.InteractionTypeCode,
            model.InteractionModesCount)).ToArray();
    }

    public static IReadOnlyList<BadgeDefinition> LoadBadgeDefinitions() =>
        Load<List<BadgeDefinition>>("badge-definitions.json");

    public static IReadOnlyList<CatalogCampaignDefinition> LoadCatalogCampaigns()
    {
        IReadOnlyList<CatalogCampaignSeedModel> models = Load<List<CatalogCampaignSeedModel>>("catalog-campaigns.json");
        return models.Select(model => new CatalogCampaignDefinition(
            model.CampaignKey,
            new CatalogPageId(model.CatalogPageId),
            model.CampaignType,
            model.Title,
            model.Subtitle,
            model.ImagePath,
            model.BadgeLabel,
            model.IsVisible,
            model.OrderNumber)).ToArray();
    }

    public static IReadOnlyList<CatalogFeatureState> LoadCatalogFeatureStates()
    {
        IReadOnlyList<CatalogFeatureStateSeedModel> models = Load<List<CatalogFeatureStateSeedModel>>("catalog-feature-states.json");
        return models.Select(model => new CatalogFeatureState(
            model.FeatureKey,
            model.IsEnabled,
            model.FeaturedOfferId is null ? null : new CatalogOfferId(model.FeaturedOfferId.Value),
            DateTimeOffset.UtcNow,
            model.UpdatedBy)).ToArray();
    }

    public static IReadOnlyList<GameDefinition> LoadGameDefinitions()
    {
        IReadOnlyList<GameDefinitionSeedModel> models = Load<List<GameDefinitionSeedModel>>("game-definitions.json");
        return models.Select(model => new GameDefinition(
            model.GameKey,
            model.Family,
            model.DisplayName,
            model.Description,
            model.IconAssetPath,
            model.SupportsPublicVenues,
            model.SupportsPrivateVenues,
            model.IsTeamBased,
            model.IsEnabled,
            model.Tags)).ToArray();
    }

    public static IReadOnlyList<GameVenueDefinition> LoadGameVenues()
    {
        IReadOnlyList<GameVenueSeedModel> models = Load<List<GameVenueSeedModel>>("game-venues.json");
        return models.Select(model => new GameVenueDefinition(
            model.VenueKey,
            model.GameKey,
            model.VenueKind,
            model.HostRoomKind,
            new RoomId(model.RoomId),
            model.DisplayName,
            model.AssetPackageKey,
            model.IsRecommended,
            model.Tags)).ToArray();
    }

    public static IReadOnlyList<PublicRoomBehaviorDefinition> LoadPublicRoomBehaviors() =>
        Load<List<PublicRoomBehaviorDefinition>>("public-room-behaviors.json");

    public static IReadOnlyList<VoucherDefinition> LoadVoucherDefinitions() =>
        Load<List<VoucherDefinition>>("vouchers.json");

    public static IReadOnlyList<CollectibleDefinition> LoadCollectibleDefinitions() =>
        Load<List<CollectibleDefinition>>("collectibles.json");

    public static IReadOnlyList<EcotronRewardDefinition> LoadEcotronRewards()
    {
        IReadOnlyList<EcotronRewardSeedModel> models = Load<List<EcotronRewardSeedModel>>("ecotron-rewards.json");
        return models.Select(model => new EcotronRewardDefinition(
            model.RewardKey,
            model.DisplayName,
            new ItemDefinitionId(model.ItemDefinitionId),
            model.RequiredRecycleCount,
            model.RewardTier,
            model.IsVisibleInCatalog,
            model.Tags)).ToArray();
    }

    private static T Load<T>(string fileName)
    {
        Assembly assembly = typeof(EmbeddedSeedContentLoader).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith($".SeedData.{fileName}", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded seed resource '{fileName}' could not be found.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed stream '{resourceName}' could not be opened.");
        T? model = JsonSerializer.Deserialize<T>(stream, JsonOptions);
        return model ?? throw new InvalidOperationException($"Embedded seed resource '{fileName}' could not be deserialized.");
    }

    private sealed record CatalogFeatureStateSeedModel(
        string FeatureKey,
        bool IsEnabled,
        long? FeaturedOfferId,
        string UpdatedBy);

    private sealed record ItemDefinitionSeedModel(
        long ItemDefinitionId,
        string PublicName,
        string InternalName,
        string ItemTypeCode,
        int SpriteId,
        double StackHeight,
        bool CanStack,
        bool CanSit,
        bool IsWalkable,
        bool AllowRecycle,
        bool AllowTrade,
        bool AllowMarketplaceSell,
        bool AllowGift,
        bool AllowInventoryStack,
        string InteractionTypeCode,
        int InteractionModesCount);

    private sealed record CatalogCampaignSeedModel(
        string CampaignKey,
        long CatalogPageId,
        string CampaignType,
        string Title,
        string Subtitle,
        string ImagePath,
        string BadgeLabel,
        bool IsVisible,
        int OrderNumber);

    private sealed record GameDefinitionSeedModel(
        string GameKey,
        GameFamily Family,
        string DisplayName,
        string Description,
        string IconAssetPath,
        bool SupportsPublicVenues,
        bool SupportsPrivateVenues,
        bool IsTeamBased,
        bool IsEnabled,
        IReadOnlyList<string> Tags);

    private sealed record GameVenueSeedModel(
        string VenueKey,
        string GameKey,
        GameVenueKind VenueKind,
        RoomKind HostRoomKind,
        long RoomId,
        string DisplayName,
        string AssetPackageKey,
        bool IsRecommended,
        IReadOnlyList<string> Tags);

    private sealed record EcotronRewardSeedModel(
        string RewardKey,
        string DisplayName,
        long ItemDefinitionId,
        int RequiredRecycleCount,
        int RewardTier,
        bool IsVisibleInCatalog,
        IReadOnlyList<string> Tags);
}
