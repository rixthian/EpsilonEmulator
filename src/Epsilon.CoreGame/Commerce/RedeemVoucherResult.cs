namespace Epsilon.CoreGame;

public sealed record RedeemVoucherResult(
    bool Succeeded,
    string Message,
    WalletSnapshot? Wallet);
