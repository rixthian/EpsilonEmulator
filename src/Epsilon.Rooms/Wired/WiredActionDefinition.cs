namespace Epsilon.Rooms.Wired;

public sealed record WiredActionDefinition(
    int SlotId,
    string ActionTypeCode,
    string ActionData,
    long? ItemId);

