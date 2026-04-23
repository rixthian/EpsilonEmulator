namespace Epsilon.CoreGame;

public interface IHabbowoodRepository
{
    ValueTask<HabbowoodEventDefinition?> GetDefinitionAsync(
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodEventDefinition> StoreDefinitionAsync(
        HabbowoodEventDefinition definition,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HabbowoodAssetPackage>> ListAssetPackagesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HabbowoodMovieSubmission>> ListSubmissionsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodMovieSubmission?> GetSubmissionBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodMovieSubmission> CreateSubmissionAsync(
        SubmitHabbowoodMovieRequest request,
        string directorName,
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask<HabbowoodMovieSubmission> StoreSubmissionAsync(
        HabbowoodMovieSubmission submission,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<HabbowoodVoteLedgerEntry>> ListVotesAsync(
        CancellationToken cancellationToken = default);

    ValueTask AppendVoteAsync(
        HabbowoodVoteLedgerEntry vote,
        CancellationToken cancellationToken = default);
}
