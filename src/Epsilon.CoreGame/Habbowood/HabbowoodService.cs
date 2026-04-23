using System.Security.Cryptography;
using System.Text;

namespace Epsilon.CoreGame;

public sealed class HabbowoodService : IHabbowoodService
{
    private readonly IHabbowoodRepository _repository;
    private readonly ICharacterProfileRepository _characterProfiles;
    private readonly IHotelEventBus _hotelEventBus;

    public HabbowoodService(
        IHabbowoodRepository repository,
        ICharacterProfileRepository characterProfiles,
        IHotelEventBus hotelEventBus)
    {
        _repository = repository;
        _characterProfiles = characterProfiles;
        _hotelEventBus = hotelEventBus;
    }

    public async ValueTask<HabbowoodEventSnapshot?> GetSnapshotAsync(
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken = default)
    {
        HabbowoodEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return null;
        }

        IReadOnlyList<HabbowoodAssetPackage> packages = await _repository.ListAssetPackagesAsync(cancellationToken);
        IReadOnlyList<HabbowoodLeaderboardEntry> leaderboard = await GetLeaderboardAsync(cancellationToken);
        HabbowoodViewerPolicy policy = await BuildViewerPolicyAsync(definition, viewerCharacterId, cancellationToken);

        return new HabbowoodEventSnapshot(definition, packages, leaderboard, policy);
    }

    public async ValueTask<IReadOnlyList<HabbowoodLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HabbowoodMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        return submissions
            .Where(candidate => candidate.Status == HabbowoodSubmissionStatus.Published)
            .OrderByDescending(candidate => candidate.VoteScore)
            .ThenBy(candidate => candidate.CreatedAtUtc)
            .Take(20)
            .Select(candidate => new HabbowoodLeaderboardEntry(
                candidate.SubmissionId,
                candidate.Slug,
                candidate.Title,
                candidate.DirectorName,
                candidate.VoteScore,
                candidate.CreatedAtUtc))
            .ToArray();
    }

    public async ValueTask<HabbowoodSubmissionSnapshot?> GetSubmissionAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        HabbowoodMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        return submission is null ? null : await BuildSnapshotAsync(submission, cancellationToken);
    }

    public async ValueTask<IReadOnlyList<HabbowoodSubmissionSnapshot>> GetSubmissionsAsync(
        bool includePending,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HabbowoodMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        IEnumerable<HabbowoodMovieSubmission> query = includePending
            ? submissions
            : submissions.Where(candidate => candidate.Status == HabbowoodSubmissionStatus.Published);

        List<HabbowoodSubmissionSnapshot> result = [];
        foreach (HabbowoodMovieSubmission submission in query
                     .OrderByDescending(candidate => candidate.VoteScore)
                     .ThenByDescending(candidate => candidate.CreatedAtUtc))
        {
            result.Add(await BuildSnapshotAsync(submission, cancellationToken));
        }

        return result;
    }

    public async ValueTask<SubmitHabbowoodMovieResult> SubmitAsync(
        SubmitHabbowoodMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        HabbowoodEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return new SubmitHabbowoodMovieResult(false, "event_missing", null);
        }

        if (!definition.IsActive)
        {
            return new SubmitHabbowoodMovieResult(false, "event_inactive", null);
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Synopsis) ||
            string.IsNullOrWhiteSpace(request.ScriptPayload))
        {
            return new SubmitHabbowoodMovieResult(false, "submission_invalid", null);
        }

        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (profile is null)
        {
            return new SubmitHabbowoodMovieResult(false, "character_missing", null);
        }

        IReadOnlyList<HabbowoodMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        int submissionCount = submissions.Count(candidate => candidate.CharacterId == request.CharacterId);
        if (submissionCount >= definition.MaxSubmissionsPerCharacter)
        {
            return new SubmitHabbowoodMovieResult(false, "submission_limit_reached", null);
        }

        string normalizedSlug = Slugify(request.Title);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new SubmitHabbowoodMovieResult(false, "title_invalid", null);
        }

        HabbowoodMovieSubmission? existing = await _repository.GetSubmissionBySlugAsync(normalizedSlug, cancellationToken);
        if (existing is not null)
        {
            return new SubmitHabbowoodMovieResult(false, "title_taken", null);
        }

        HabbowoodMovieSubmission created = await _repository.CreateSubmissionAsync(
            request,
            profile.Username,
            normalizedSlug,
            cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.HabbowoodSubmissionChanged,
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

        return new SubmitHabbowoodMovieResult(true, null, await BuildSnapshotAsync(created, cancellationToken));
    }

    public async ValueTask<VoteHabbowoodMovieResult> VoteAsync(
        VoteHabbowoodMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        HabbowoodEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return new VoteHabbowoodMovieResult(false, "event_missing", null);
        }

        if (!definition.IsActive)
        {
            return new VoteHabbowoodMovieResult(false, "event_inactive", null);
        }

        if (request.VoteDelta is not 1 and not -1)
        {
            return new VoteHabbowoodMovieResult(false, "vote_invalid", null);
        }

        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(request.CharacterId, cancellationToken);
        if (profile is null)
        {
            return new VoteHabbowoodMovieResult(false, "character_missing", null);
        }

        HabbowoodMovieSubmission? submission =
            await _repository.GetSubmissionBySlugAsync(request.SubmissionSlug, cancellationToken);
        if (submission is null || submission.Status != HabbowoodSubmissionStatus.Published)
        {
            return new VoteHabbowoodMovieResult(false, "submission_not_votable", null);
        }

        IReadOnlyList<HabbowoodVoteLedgerEntry> votes = await _repository.ListVotesAsync(cancellationToken);
        DateTime thresholdUtc = DateTime.UtcNow.AddHours(-1);
        int voteCount = votes.Count(candidate =>
            candidate.CharacterId == request.CharacterId &&
            candidate.CreatedAtUtc >= thresholdUtc);
        if (voteCount >= definition.MaxVotesPerHour)
        {
            return new VoteHabbowoodMovieResult(false, "vote_limit_reached", submission.VoteScore);
        }

        string? remoteAddressHash = string.IsNullOrWhiteSpace(request.RemoteAddress)
            ? null
            : HashRemoteAddress(request.RemoteAddress);

        await _repository.AppendVoteAsync(
            new HabbowoodVoteLedgerEntry(
                submission.SubmissionId,
                request.CharacterId,
                request.VoteDelta,
                remoteAddressHash,
                DateTime.UtcNow),
            cancellationToken);

        HabbowoodMovieSubmission updated = submission with
        {
            VoteScore = submission.VoteScore + request.VoteDelta
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.HabbowoodVoteRecorded,
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

        return new VoteHabbowoodMovieResult(true, null, updated.VoteScore);
    }

    public async ValueTask<HabbowoodSubmissionSnapshot?> PublishAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default)
    {
        HabbowoodMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        HabbowoodMovieSubmission updated = submission with
        {
            Status = HabbowoodSubmissionStatus.Published,
            PublishedAtUtc = DateTime.UtcNow,
            RejectedAtUtc = null,
            ModerationNote = NormalizeOptional(moderationNote)
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.HabbowoodSubmissionChanged,
            moderatorCharacterId,
            null,
            new { updated.SubmissionId, updated.Slug, updated.Status },
            cancellationToken);

        return await BuildSnapshotAsync(updated, cancellationToken);
    }

    public async ValueTask<HabbowoodSubmissionSnapshot?> RejectAsync(
        string slug,
        CharacterId moderatorCharacterId,
        string? moderationNote,
        CancellationToken cancellationToken = default)
    {
        HabbowoodMovieSubmission? submission = await _repository.GetSubmissionBySlugAsync(slug, cancellationToken);
        if (submission is null)
        {
            return null;
        }

        HabbowoodMovieSubmission updated = submission with
        {
            Status = HabbowoodSubmissionStatus.Rejected,
            RejectedAtUtc = DateTime.UtcNow,
            ModerationNote = NormalizeOptional(moderationNote)
        };
        await _repository.StoreSubmissionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.HabbowoodSubmissionChanged,
            moderatorCharacterId,
            null,
            new { updated.SubmissionId, updated.Slug, updated.Status },
            cancellationToken);

        return await BuildSnapshotAsync(updated, cancellationToken);
    }

    public async ValueTask<HabbowoodEventDefinition?> SetActivationAsync(
        bool isActive,
        CharacterId moderatorCharacterId,
        CancellationToken cancellationToken = default)
    {
        HabbowoodEventDefinition? definition = await _repository.GetDefinitionAsync(cancellationToken);
        if (definition is null)
        {
            return null;
        }

        HabbowoodEventDefinition updated = definition with
        {
            IsActive = isActive,
            ActivatedAtUtc = DateTime.UtcNow,
            ActivatedByCharacterId = moderatorCharacterId
        };
        await _repository.StoreDefinitionAsync(updated, cancellationToken);

        await PublishHotelEventAsync(
            HotelEventKind.HabbowoodActivationChanged,
            moderatorCharacterId,
            updated.LobbyRoomId,
            new { updated.EventKey, updated.IsActive },
            cancellationToken);

        return updated;
    }

    private async ValueTask<HabbowoodViewerPolicy> BuildViewerPolicyAsync(
        HabbowoodEventDefinition definition,
        CharacterId viewerCharacterId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<HabbowoodVoteLedgerEntry> votes = await _repository.ListVotesAsync(cancellationToken);
        IReadOnlyList<HabbowoodMovieSubmission> submissions = await _repository.ListSubmissionsAsync(cancellationToken);
        DateTime thresholdUtc = DateTime.UtcNow.AddHours(-1);

        int usedVotes = votes.Count(candidate =>
            candidate.CharacterId == viewerCharacterId &&
            candidate.CreatedAtUtc >= thresholdUtc);
        int usedSubmissions = submissions.Count(candidate => candidate.CharacterId == viewerCharacterId);

        int remainingVotes = Math.Max(0, definition.MaxVotesPerHour - usedVotes);
        int remainingSubmissionSlots = Math.Max(0, definition.MaxSubmissionsPerCharacter - usedSubmissions);

        return new HabbowoodViewerPolicy(
            remainingVotes,
            remainingSubmissionSlots,
            definition.IsActive && remainingSubmissionSlots > 0,
            definition.IsActive && remainingVotes > 0);
    }

    private async ValueTask<HabbowoodSubmissionSnapshot> BuildSnapshotAsync(
        HabbowoodMovieSubmission submission,
        CancellationToken cancellationToken)
    {
        CharacterProfile? profile = await _characterProfiles.GetByIdAsync(submission.CharacterId, cancellationToken);
        return new HabbowoodSubmissionSnapshot(
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
