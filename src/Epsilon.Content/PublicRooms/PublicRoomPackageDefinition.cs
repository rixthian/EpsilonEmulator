namespace Epsilon.Content;

public sealed record PublicRoomPackageDefinition(
    string AssetPackageKey,
    string AssetFamily,
    string VisualProfileKey,
    string BaseLayoutCode,
    IReadOnlyList<string> AssetLayerKeys,
    IReadOnlyList<string> Tags);
