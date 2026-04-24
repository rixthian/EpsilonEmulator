using Epsilon.CoreGame;

namespace Epsilon.Persistence;

internal sealed class InMemoryStudioRepository : IStudioRepository
{
    private readonly InMemoryHotelStore _store;
    private readonly object _sync = new();

    public InMemoryStudioRepository(InMemoryHotelStore store)
    {
        _store = store;
    }

    public ValueTask<StudioEventDefinition?> GetDefinitionAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult(_store.StudioDefinition);
        }
    }

    public ValueTask<StudioEventDefinition> StoreDefinitionAsync(
        StudioEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.StudioDefinition = definition;
            return ValueTask.FromResult(definition);
        }
    }

    public ValueTask<IReadOnlyList<StudioAssetPackage>> ListAssetPackagesAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<StudioAssetPackage>>([.. _store.StudioAssetPackages]);
        }
    }

    public ValueTask<IReadOnlyList<StudioMovieSubmission>> ListSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<StudioMovieSubmission>>([.. _store.StudioSubmissions]);
        }
    }

    public ValueTask<StudioMovieSubmission?> GetSubmissionBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            StudioMovieSubmission? submission = _store.StudioSubmissions
                .FirstOrDefault(candidate => string.Equals(candidate.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<StudioMovieSubmission> CreateSubmissionAsync(
        SubmitStudioMovieRequest request,
        string directorName,
        string slug,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            StudioMovieSubmission submission = new(
                new StudioSubmissionId(_store.NextStudioSubmissionId++),
                "studio",
                request.CharacterId,
                directorName,
                request.Title.Trim(),
                slug,
                request.Synopsis.Trim(),
                request.ScriptPayload.Trim(),
                string.IsNullOrWhiteSpace(request.ContactHandle) ? null : request.ContactHandle.Trim(),
                string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim(),
                StudioSubmissionStatus.Pending,
                0,
                DateTime.UtcNow,
                null,
                null,
                null);

            _store.StudioSubmissions.Add(submission);
            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<StudioMovieSubmission> StoreSubmissionAsync(
        StudioMovieSubmission submission,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int existingIndex = _store.StudioSubmissions.FindIndex(candidate =>
                candidate.SubmissionId == submission.SubmissionId);

            if (existingIndex >= 0)
            {
                _store.StudioSubmissions[existingIndex] = submission;
            }
            else
            {
                _store.StudioSubmissions.Add(submission);
            }

            return ValueTask.FromResult(submission);
        }
    }

    public ValueTask<IReadOnlyList<StudioVoteLedgerEntry>> ListVotesAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return ValueTask.FromResult<IReadOnlyList<StudioVoteLedgerEntry>>([.. _store.StudioVotes]);
        }
    }

    public ValueTask AppendVoteAsync(
        StudioVoteLedgerEntry vote,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _store.StudioVotes.Add(vote);
            return ValueTask.CompletedTask;
        }
    }
}
