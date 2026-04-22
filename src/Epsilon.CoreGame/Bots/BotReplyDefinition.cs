namespace Epsilon.CoreGame;

public sealed record BotReplyDefinition(
    string ReplyKey,
    IReadOnlyList<string> TriggerKeywords,
    string ResponseText,
    int? GrantedCarryItemTypeId,
    string? GrantedCarryItemName);
