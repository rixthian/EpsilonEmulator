namespace Epsilon.CoreGame;

public enum StudioSubmissionStatus
{
    Pending = 0,
    Published = 1,
    Rejected = 2
}

public sealed record StudioAssetPackage(
    string PackageKey,
    string DisplayName,
    string AssetKind,
    int AssetCount,
    string Notes);

public sealed record StudioEventDefinition(
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

public sealed record StudioMovieSubmission(
    StudioSubmissionId SubmissionId,
    string EventKey,
    CharacterId CharacterId,
    string DirectorName,
    string Title,
    string Slug,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string LanguageCode,
    StudioSubmissionStatus Status,
    int VoteScore,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? RejectedAtUtc,
    string? ModerationNote);

public sealed record StudioVoteLedgerEntry(
    StudioSubmissionId SubmissionId,
    CharacterId CharacterId,
    int VoteDelta,
    string? RemoteAddressHash,
    DateTime CreatedAtUtc);

public sealed record StudioLeaderboardEntry(
    StudioSubmissionId SubmissionId,
    string Slug,
    string Title,
    string DirectorName,
    int VoteScore,
    DateTime CreatedAtUtc);

public sealed record StudioSubmissionSnapshot(
    StudioMovieSubmission Submission,
    string AuthorFigure,
    string AuthorMotto);

public sealed record StudioViewerPolicy(
    int RemainingHourlyVotes,
    int RemainingSubmissionSlots,
    bool CanSubmit,
    bool CanVote);

public sealed record StudioEventSnapshot(
    StudioEventDefinition Definition,
    IReadOnlyList<StudioAssetPackage> AssetPackages,
    IReadOnlyList<StudioLeaderboardEntry> Leaderboard,
    StudioViewerPolicy ViewerPolicy);

public sealed record SubmitStudioMovieRequest(
    CharacterId CharacterId,
    string Title,
    string Synopsis,
    string ScriptPayload,
    string? ContactHandle,
    string LanguageCode);

public sealed record SubmitStudioMovieResult(
    bool Succeeded,
    string? FailureCode,
    StudioSubmissionSnapshot? Snapshot);

public sealed record VoteStudioMovieRequest(
    CharacterId CharacterId,
    string SubmissionSlug,
    int VoteDelta,
    string? RemoteAddress);

public sealed record VoteStudioMovieResult(
    bool Succeeded,
    string? FailureCode,
    int? CurrentVoteScore);
