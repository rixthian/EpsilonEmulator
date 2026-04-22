namespace Epsilon.CoreGame;

public sealed record SupportTopicEntry(
    int TopicId,
    string Title,
    string Body,
    int CategoryId,
    bool IsKnownIssue,
    bool IsImportant);
