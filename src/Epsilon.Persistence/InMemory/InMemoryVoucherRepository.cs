using Epsilon.Content;

namespace Epsilon.Persistence;

internal sealed class InMemoryVoucherRepository : IVoucherRepository
{
    private readonly InMemoryHotelStore _store;

    public InMemoryVoucherRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<IReadOnlyList<VoucherDefinition>> GetEnabledAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VoucherDefinition> vouchers = _store.VoucherDefinitions
            .Where(candidate => candidate.IsEnabled)
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return ValueTask.FromResult(vouchers);
    }

    public ValueTask<VoucherDefinition?> GetByCodeAsync(
        string voucherCode,
        CancellationToken cancellationToken = default)
    {
        VoucherDefinition? voucher = _store.VoucherDefinitions.FirstOrDefault(candidate =>
            string.Equals(candidate.VoucherCode, voucherCode, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(voucher);
    }
}
