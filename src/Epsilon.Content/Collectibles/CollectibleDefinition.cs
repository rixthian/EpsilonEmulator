namespace Epsilon.Content;

public sealed record CollectibleDefinition(
    string CollectibleKey,
    string DisplayName,
    string CategoryKey,
    string ImagePath,
    bool IsSeasonal,
    bool IsTradable,
    bool IsVisible,
    IReadOnlyList<string> Tags);
