using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryHabbowoodRepository : IHabbowoodRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryHabbowoodRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<HabbowoodEventDefinition?> GetDefinitionAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult(_store.HabbowoodDefinition);
        }
    }

    public ValueTask<HabbowoodEventDefinition> StoreDefinitionAsync(
        HabbowoodEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.HabbowoodDefinition = definition;
            return ValueTask.FromResult(definition);
        }
    }

    public ValueTask<IReadOnlyList<HabbowoodAssetPackage>> ListAssetPackagesAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<HabbowoodAssetPackage>>([.. _store.HabbowoodAssetPackages]);
        }
    }

    public ValueTask<IReadOnlyList<HabbowoodMovieSubmission>> ListSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<HabbowoodMovieSubmission>>([.. _store.HabbowoodSubmissions]);
        }
    }

    public ValueTask<HabbowoodMovieSubmission?> GetSubmissionBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            HabbowoodMovieSubmission? submission = _store.HabbowoodSubmissions
                .FirstOrDefault(candidate => string.Equals(candidate.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<HabbowoodMovieSubmission> CreateSubmissionAsync(
        SubmitHabbowoodMovieRequest request,
        string directorName,
        string slug,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            HabbowoodMovieSubmission submission = new(
                new HabbowoodSubmissionId(_store.NextHabbowoodSubmissionId++),
                "habbowood",
                request.CharacterId,
                directorName,
                request.Title.Trim(),
                slug,
                request.Synopsis.Trim(),
                request.ScriptPayload.Trim(),
                string.IsNullOrWhiteSpace(request.ContactHandle) ? null : request.ContactHandle.Trim(),
                string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim(),
                HabbowoodSubmissionStatus.Pending,
                0,
                DateTime.UtcNow,
                null,
                null,
                null);

            _store.HabbowoodSubmissions.Add(submission);
            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<HabbowoodMovieSubmission> StoreSubmissionAsync(
        HabbowoodMovieSubmission submission,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int existingIndex = _store.HabbowoodSubmissions.FindIndex(candidate =>
                candidate.SubmissionId == submission.SubmissionId);

            if (existingIndex >= 0)
            {
                _store.HabbowoodSubmissions[existingIndex] = submission;
            }
            else
            {
                _store.HabbowoodSubmissions.Add(submission);
            }

            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<IReadOnlyList<HabbowoodVoteLedgerEntry>> ListVotesAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<HabbowoodVoteLedgerEntry>>([.. _store.HabbowoodVotes]);
        }
    }

    public ValueTask AppendVoteAsync(
        HabbowoodVoteLedgerEntry vote,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.HabbowoodVotes.Add(vote);
            return ValueTask.CompletedTask;
        }
    }
}
