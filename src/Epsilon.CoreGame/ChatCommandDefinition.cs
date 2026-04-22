namespace Epsilon.CoreGame;

public sealed record ChatCommandDefinition(
    string CommandKey,
    string Description,
    ChatCommandScope Scope,
    bool RequiresRoomContext,
    IReadOnlyList<string> Aliases,
    string? RequiredCapabilityKey);
