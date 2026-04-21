using Epsilon.CoreGame;

namespace Epsilon.Rooms.Wired;

public sealed record WiredTriggerDefinition(
    int SlotId,
    RoomId RoomId,
    string TriggerTypeCode,
    string TriggerData);

