using Epsilon.Content;
using Epsilon.CoreGame;

namespace Epsilon.Launcher;

public sealed record LauncherBootstrapSnapshot(
    LauncherClientProfileSnapshot Profile,
    LauncherConnectionPolicy ConnectionPolicy,
    ClientPackageManifest Package,
    string GatewayBaseUrl,
    string EntryAssetUrl,
    string AssetBaseUrl,
    InterfacePreferenceSnapshot? InterfacePreferences,
    IReadOnlyList<InterfaceLanguageDefinition> SupportedLanguages,
    LauncherSessionSnapshot? Session,
    IReadOnlyDictionary<string, string> EndpointMap);
