namespace Epsilon.CoreGame;

public interface IStudioService
{
    ValueTask<StudioEventSnapshot?> GetSnapshotAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StudioLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default);

    ValueTask<StudioSubmissionSnapshot?> GetSubmissionAsync(
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StudioSubmissionSnapshot>> GetSubmissionsAsync(
        bool includePending,
        CancellationToken cancellationToken = default);

    ValueTask<SubmitStudioMovieResult> SubmitAsync(
        SubmitStudioMovieRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<VoteStudioMovieResult> VoteAsync(
        VoteStudioMovieRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<StudioSubmissionSnapshot?> PublishAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default);

    ValueTask<StudioSubmissionSnapshot?> RejectAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default);

    ValueTask<StudioEventDefinition?> SetActivationAsync(
        bool isActive,
        CharacterId moderatorCharacterId,
        CancellationToken cancellationToken = default);
}
