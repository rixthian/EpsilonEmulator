namespace Epsilon.CoreGame;

public sealed record SupportCenterSnapshot(
    IReadOnlyList<SupportTopicCategory> Categories,
    IReadOnlyList<SupportTopicEntry> ImportantTopics,
    IReadOnlyList<SupportTopicEntry> KnownIssues,
    IReadOnlyList<SupportCallTicket> OpenTickets);
