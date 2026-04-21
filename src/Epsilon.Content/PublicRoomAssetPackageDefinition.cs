namespace Epsilon.Content;

public sealed record PublicRoomAssetPackageDefinition(
    string AssetPackageKey,
    string AssetFamily,
    string VisualProfileKey,
    string BaseLayoutCode,
    IReadOnlyList<string> AssetLayerKeys,
    IReadOnlyList<string> Tags);
