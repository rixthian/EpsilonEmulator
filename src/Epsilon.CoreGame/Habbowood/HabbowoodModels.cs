namespace Epsilon.CoreGame;

public enum HabbowoodSubmissionStatus
{
    Pending = 0,
    Published = 1,
    Rejected = 2
}

public sealed record HabbowoodAssetPackage(
    string PackageKey,
    string DisplayName,
    string AssetKind,
    int AssetCount,
    string Notes);

public sealed record HabbowoodEventDefinition(
    string EventKey,
    string DisplayName,
    string Slug,
    string Description,
    RoomId LobbyRoomId,
    bool IsActive,
    int MaxVotesPerHour,
    int MaxSubmissionsPerCharacter,
    DateTime CreatedAtUtc,
    DateTime? ActivatedAtUtc,
    CharacterId? ActivatedByCharacterId);

public sealed record HabbowoodMovieSubmission(
    HabbowoodSubmissionId SubmissionId,
    string EventKey,
    CharacterId CharacterId,
    string DirectorName,
    string Title,
    string Slug,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string LanguageCode,
    HabbowoodSubmissionStatus Status,
    int VoteScore,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? RejectedAtUtc,
    string? ModerationNote);

public sealed record HabbowoodVoteLedgerEntry(
    HabbowoodSubmissionId SubmissionId,
    CharacterId CharacterId,
    int VoteDelta,
    string? RemoteAddressHash,
    DateTime CreatedAtUtc);

public sealed record HabbowoodLeaderboardEntry(
    HabbowoodSubmissionId SubmissionId,
    string Slug,
    string Title,
    string DirectorName,
    int VoteScore,
    DateTime CreatedAtUtc);

public sealed record HabbowoodSubmissionSnapshot(
    HabbowoodMovieSubmission Submission,
    string AuthorFigure,
    string AuthorMotto);

public sealed record HabbowoodViewerPolicy(
    int RemainingHourlyVotes,
    int RemainingSubmissionSlots,
    bool CanSubmit,
    bool CanVote);

public sealed record HabbowoodEventSnapshot(
    HabbowoodEventDefinition Definition,
    IReadOnlyList<HabbowoodAssetPackage> AssetPackages,
    IReadOnlyList<HabbowoodLeaderboardEntry> Leaderboard,
    HabbowoodViewerPolicy ViewerPolicy);

public sealed record SubmitHabbowoodMovieRequest(
    CharacterId CharacterId,
    string Title,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string LanguageCode);

public sealed record SubmitHabbowoodMovieResult(
    bool Succeeded,
    string? FailureCode,
    HabbowoodSubmissionSnapshot? Snapshot);

public sealed record VoteHabbowoodMovieRequest(
    CharacterId CharacterId,
    string SubmissionSlug,
    int VoteDelta,
    string? RemoteAddress);

public sealed record VoteHabbowoodMovieResult(
    bool Succeeded,
    string? FailureCode,
    int? CurrentVoteScore);
