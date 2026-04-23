namespace Epsilon.Gateway;

public sealed record DevCollectibleOwnershipInput(
    IReadOnlyList<string>? CollectibleKeys,
    IReadOnlyList<string>? CategoryKeys);
