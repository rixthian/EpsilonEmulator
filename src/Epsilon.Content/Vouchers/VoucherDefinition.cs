namespace Epsilon.Content;

public sealed record VoucherDefinition(
    string VoucherCode,
    string DisplayName,
    string Description,
    string RewardCurrencyCode,
    int RewardAmount,
    bool IsEnabled,
    IReadOnlyList<string> Tags);
