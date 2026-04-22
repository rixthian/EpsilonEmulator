namespace Epsilon.Rooms;

public sealed record RoomVisualSceneDefinition(
    string SceneKey,
    string LayoutCode,
    string ThemeKey,
    string AmbientColorHex,
    string ShadowProfileKey,
    string LightingPresetKey,
    string SkyAnimationKey,
    bool EnableCloudMotion,
    bool EnableParallaxBackdrop,
    IReadOnlyList<string> AmbientEffectKeys);
