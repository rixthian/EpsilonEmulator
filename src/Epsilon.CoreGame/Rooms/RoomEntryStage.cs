namespace Epsilon.CoreGame;

public enum RoomEntryStage
{
    Requested = 0,
    CharacterResolved = 1,
    RoomResolved = 2,
    AccessValidated = 3,
    LayoutValidated = 4,
    ContentPrepared = 5,
    ContextPrepared = 6,
    RuntimePresenceRegistered = 7,
    Completed = 8,
    Failed = 9
}
