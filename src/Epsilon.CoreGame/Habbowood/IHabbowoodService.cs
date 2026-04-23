namespace Epsilon.CoreGame;

public interface IHabbowoodService
{
    ValueTask<HabbowoodEventSnapshot?> GetSnapshotAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HabbowoodLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodSubmissionSnapshot?> GetSubmissionAsync(
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HabbowoodSubmissionSnapshot>> GetSubmissionsAsync(
        bool includePending,
        CancellationToken cancellationToken = default);

    ValueTask<SubmitHabbowoodMovieResult> SubmitAsync(
        SubmitHabbowoodMovieRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<VoteHabbowoodMovieResult> VoteAsync(
        VoteHabbowoodMovieRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodSubmissionSnapshot?> PublishAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodSubmissionSnapshot?> RejectAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodEventDefinition?> SetActivationAsync(
        bool isActive,
        CharacterId moderatorCharacterId,
        CancellationToken cancellationToken = default);
}
