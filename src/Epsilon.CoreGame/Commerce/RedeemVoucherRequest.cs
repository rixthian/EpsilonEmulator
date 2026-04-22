namespace Epsilon.CoreGame;

public sealed record RedeemVoucherRequest(
    CharacterId CharacterId,
    string VoucherCode);
