namespace Epsilon.CoreGame;

public sealed record WalletAdjustedEvent(
    CharacterId SourceCharacterId,
    CharacterId TargetCharacterId,
    string CurrencyCode,
    int Delta,
    string Reason);
