namespace Epsilon.CoreGame;

public interface ISupportCenterService
{
    ValueTask<SupportCenterSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);

    ValueTask<SupportCallResult> CreateCallAsync(
        SupportCallRequest request,
        CancellationToken cancellationToken = default);
}
