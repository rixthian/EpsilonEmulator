namespace Epsilon.Content;

public interface IVoucherRepository
{
    ValueTask<IReadOnlyList<VoucherDefinition>> GetEnabledAsync(
        CancellationToken cancellationToken = default);

    ValueTask<VoucherDefinition?> GetByCodeAsync(
        string voucherCode,
        CancellationToken cancellationToken = default);
}
