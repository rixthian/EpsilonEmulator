namespace Epsilon.Content;

public sealed record ClientPackageManifest(
    string PackageKey,
    string PackageFamily,
    string VersionKey,
    string EntryAssetPath,
    string AssetBasePath,
    IReadOnlyList<string> ExternalVariableKeys,
    IReadOnlyList<string> ExternalTextKeys,
    IReadOnlyList<string> PublicRoomAssetPackageKeys,
    IReadOnlyList<string> FeatureFlags);
