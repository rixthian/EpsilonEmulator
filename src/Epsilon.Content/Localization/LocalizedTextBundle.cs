namespace Epsilon.Content;

public sealed record LocalizedTextBundle(
    string BundleKey,
    string LanguageCode,
    string SurfaceKey,
    IReadOnlyDictionary<string, string> Entries,
    DateTimeOffset UpdatedAt);
