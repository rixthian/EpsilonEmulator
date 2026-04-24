using System.Security.Cryptography;
using System.Text;

namespace Epsilon.CoreGame;

public sealed class StudioService : IStudioService
{
    private readonly IStudioRepository _repository;
    private readonly ICharacterProfileRepository _characterProfiles;
    private readonly IHotelEventBus _hotelEventBus;

    public StudioService(
        IStudioRepository repository,
        ICharacterProfileRepository characterProfiles,
        IHotelEventBus hotelEventBus)
    {
        _repository = repository;
        _characterProfiles = characterProfiles;
        _hotelEventBus = hotelEventBus;
    }

    public async ValueTask<StudioEventSnapshot?> GetSnapshotAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default)
    {
        StudioEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return null;
        }

        IReadOnlyList<StudioAssetPackage> packages = await _repository.ListAssetPackagesAsync(cancellationToken);
        IReadOnlyList<StudioLeaderboardEntry> leaderboard = await GetLeaderboardAsync(cancellationToken);
        StudioViewerPolicy policy = await BuildViewerPolicyAsync(definition, viewerCharacterId, cancellationToken);

        return new StudioEventSnapshot(definition, packages, leaderboard, policy);
    }

    public async ValueTask<IReadOnlyList<StudioLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StudioMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        return submissions
            .Where(candidate => candidate.Status == StudioSubmissionStatus.Published)
            .OrderByDescending(candidate => candidate.VoteScore)
            .ThenBy(candidate => candidate.CreatedAtUtc)
            .Take(20)
            .Select(candidate => new StudioLeaderboardEntry(
                candidate.SubmissionId,
                candidate.Slug,
                candidate.Title,
                candidate.DirectorName,
                candidate.VoteScore,
                candidate.CreatedAtUtc))
            .ToArray();
    }

    public async ValueTask<StudioSubmissionSnapshot?> GetSubmissionAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        StudioMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        return submission is null ? null : await BuildSnapshotAsync(submission, cancellationToken);
    }

    public async ValueTask<IReadOnlyList<StudioSubmissionSnapshot>> GetSubmissionsAsync(
        bool includePending,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StudioMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        IEnumerable<StudioMovieSubmission> query = includePending
            ? submissions
            : submissions.Where(candidate => candidate.Status == StudioSubmissionStatus.Published);

        List<StudioSubmissionSnapshot> result = [];
        foreach (StudioMovieSubmission submission in query
                     .OrderByDescending(candidate => candidate.VoteScore)
                     .ThenByDescending(candidate => candidate.CreatedAtUtc))
        {
            result.Add(await BuildSnapshotAsync(submission, cancellationToken));
        }

        return result;
    }

    public async ValueTask<SubmitStudioMovieResult> SubmitAsync(
        SubmitStudioMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        StudioEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return new SubmitStudioMovieResult(false, "event_missing", null);
        }

        if (!definition.IsActive)
        {
            return new SubmitStudioMovieResult(false, "event_inactive", null);
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Synopsis) ||
            string.IsNullOrWhiteSpace(request.ScriptPayload))
        {
            return new SubmitStudioMovieResult(false, "submission_invalid", null);
        }

        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (profile is null)
        {
            return new SubmitStudioMovieResult(false, "character_missing", null);
        }

        IReadOnlyList<StudioMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        int submissionCount = submissions.Count(candidate => candidate.CharacterId == request.CharacterId);
        if (submissionCount >= definition.MaxSubmissionsPerCharacter)
        {
            return new SubmitStudioMovieResult(false, "submission_limit_reached", null);
        }

        string normalizedSlug = Slugify(request.Title);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new SubmitStudioMovieResult(false, "title_invalid", null);
        }

        StudioMovieSubmission? existing = await _repository.GetSubmissionBySlugAsync(normalizedSlug, cancellationToken);
        if (existing is not null)
        {
            return new SubmitStudioMovieResult(false, "title_taken", null);
        }

        StudioMovieSubmission created = await _repository.CreateSubmissionAsync(
            request,
            profile.Username,
            normalizedSlug,
            cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.StudioSubmissionChanged,
            request.CharacterId,
            null,
            new
            {
                created.SubmissionId,
                created.Slug,
                created.Title,
                created.Status
            },
            cancellationToken);

        return new SubmitStudioMovieResult(true, null, await BuildSnapshotAsync(created, cancellationToken));
    }

    public async ValueTask<VoteStudioMovieResult> VoteAsync(
        VoteStudioMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        StudioEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return new VoteStudioMovieResult(false, "event_missing", null);
        }

        if (!definition.IsActive)
        {
            return new VoteStudioMovieResult(false, "event_inactive", null);
        }

        if (request.VoteDelta is not 1 and not -1)
        {
            return new VoteStudioMovieResult(false, "vote_invalid", null);
        }

        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (profile is null)
        {
            return new VoteStudioMovieResult(false, "character_missing", null);
        }

        StudioMovieSubmission? submission =
            await _repository.GetSubmissionBySlugAsync(request.SubmissionSlug, cancellationToken);
        if (submission is null || submission.Status != StudioSubmissionStatus.Published)
        {
            return new VoteStudioMovieResult(false, "submission_not_votable", null);
        }

        IReadOnlyList<StudioVoteLedgerEntry> votes = await _repository.ListVotesAsync(cancellationToken);
        DateTime thresholdUtc = DateTime.UtcNow.AddHours(-1);
        int voteCount = votes.Count(candidate =>
            candidate.CharacterId == request.CharacterId &&
            candidate.CreatedAtUtc >= thresholdUtc);
        if (voteCount >= definition.MaxVotesPerHour)
        {
            return new VoteStudioMovieResult(false, "vote_limit_reached", submission.VoteScore);
        }

        string? remoteAddressHash = string.IsNullOrWhiteSpace(request.RemoteAddress)
            ? null
            : HashRemoteAddress(request.RemoteAddress);

        await _repository.AppendVoteAsync(
            new StudioVoteLedgerEntry(
                submission.SubmissionId,
                request.CharacterId,
                request.VoteDelta,
                remoteAddressHash,
                DateTime.UtcNow),
            cancellationToken);

        StudioMovieSubmission updated = submission with
        {
            VoteScore = submission.VoteScore + request.VoteDelta
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.StudioVoteRecorded,
            request.CharacterId,
            null,
            new
            {
                updated.SubmissionId,
                updated.Slug,
                updated.VoteScore,
                request.VoteDelta
            },
            cancellationToken);

        return new VoteStudioMovieResult(true, null, updated.VoteScore);
    }

    public async ValueTask<StudioSubmissionSnapshot?> PublishAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default)
    {
        StudioMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        StudioMovieSubmission updated = submission with
        {
            Status = StudioSubmissionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow,
            RejectedAtUtc = null,
            ModerationNote = NormalizeOptional(moderationNote)
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.StudioSubmissionChanged,
            moderatorCharacterId,
            null,
            new { updated.SubmissionId, updated.Slug, updated.Status },
            cancellationToken);

        return await BuildSnapshotAsync(updated, cancellationToken);
    }

    public async ValueTask<StudioSubmissionSnapshot?> RejectAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default)
    {
        StudioMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        StudioMovieSubmission updated = submission with
        {
            Status = StudioSubmissionStatus.Rejected,
            RejectedAtUtc = DateTime.UtcNow,
            ModerationNote = NormalizeOptional(moderationNote)
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.StudioSubmissionChanged,
            moderatorCharacterId,
            null,
            new { updated.SubmissionId, updated.Slug, updated.Status },
            cancellationToken);

        return await BuildSnapshotAsync(updated, cancellationToken);
    }

    public async ValueTask<StudioEventDefinition?> SetActivationAsync(
        bool isActive,
        CharacterId moderatorCharacterId,
        CancellationToken cancellationToken = default)
    {
        StudioEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return null;
        }

        StudioEventDefinition updated = definition with
        {
            IsActive = isActive,
            ActivatedAtUtc = DateTime.UtcNow,
            ActivatedByCharacterId = moderatorCharacterId
        };
        await _repository.StoreDefinitionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.StudioActivationChanged,
            moderatorCharacterId,
            updated.LobbyRoomId,
            new { updated.EventKey, updated.IsActive },
            cancellationToken);

        return updated;
    }

    private async ValueTask<StudioViewerPolicy> BuildViewerPolicyAsync(
        StudioEventDefinition definition,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StudioVoteLedgerEntry> votes = await _repository.ListVotesAsync(cancellationToken);
        IReadOnlyList<StudioMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        DateTime thresholdUtc = DateTime.UtcNow.AddHours(-1);

        int usedVotes = votes.Count(candidate =>
            candidate.CharacterId == viewerCharacterId &&
            candidate.CreatedAtUtc >= thresholdUtc);
        int usedSubmissions = submissions.Count(candidate => candidate.CharacterId == viewerCharacterId);

        int remainingVotes = Math.Max(0, definition.MaxVotesPerHour - usedVotes);
        int remainingSubmissionSlots = Math.Max(0, definition.MaxSubmissionsPerCharacter - usedSubmissions);

        return new StudioViewerPolicy(
            remainingVotes,
            remainingSubmissionSlots,
            definition.IsActive && remainingSubmissionSlots > 0,
            definition.IsActive && remainingVotes > 0);
    }

    private async ValueTask<StudioSubmissionSnapshot> BuildSnapshotAsync(
        StudioMovieSubmission submission,
        CancellationToken cancellationToken)
    {
        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(submission.CharacterId, cancellationToken);
        return new StudioSubmissionSnapshot(
            submission,
            profile?.Figure ?? "hd-180-1.ch-210-66.lg-270-82.sh-290-80",
            profile?.Motto ?? string.Empty);
    }

    private async ValueTask PublishHotelEventAsync(
        HotelEventKind kind,
        CharacterId actorCharacterId,
        RoomId? roomId,
        object payload,
        CancellationToken cancellationToken)
    {
        await _hotelEventBus.PublishAsync(
            new HotelEventEnvelope(
                Guid.NewGuid(),
                kind,
                DateTime.UtcNow,
                actorCharacterId,
                roomId,
                payload),
            cancellationToken);
    }

    private static string Slugify(string value)
    {
        StringBuilder builder = new();
        bool lastWasDash = false;

        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string HashRemoteAddress(string remoteAddress)
    {
        byte[] payload = SHA256.HashData(Encoding.UTF8.GetBytes(remoteAddress.Trim()));
        return Convert.ToHexString(payload);
    }
}
