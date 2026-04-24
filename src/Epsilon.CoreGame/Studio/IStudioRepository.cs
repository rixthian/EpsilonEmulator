namespace Epsilon.CoreGame;

public interface IStudioRepository
{
    ValueTask<StudioEventDefinition?> GetDefinitionAsync(
        CancellationToken cancellationToken = default);

    ValueTask<StudioEventDefinition> StoreDefinitionAsync(
        StudioEventDefinition definition,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StudioAssetPackage>> ListAssetPackagesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StudioMovieSubmission>> ListSubmissionsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<StudioMovieSubmission?> GetSubmissionBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<StudioMovieSubmission> CreateSubmissionAsync(
        SubmitStudioMovieRequest request,
        string directorName,
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<StudioMovieSubmission> StoreSubmissionAsync(
        StudioMovieSubmission submission,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StudioVoteLedgerEntry>> ListVotesAsync(
        CancellationToken cancellationToken = default);

    ValueTask AppendVoteAsync(
        StudioVoteLedgerEntry vote,
        CancellationToken cancellationToken = default);
}
