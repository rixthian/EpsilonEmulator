namespace Epsilon.CoreGame;

public enum RoomRuntimeMutationKind
{
    ActorStateChanged = 1,
    ChatMessageAppended = 2,
    ChatPolicyChanged = 3,
    ActorPresenceChanged = 4,
    RoomContentChanged = 5,
}
