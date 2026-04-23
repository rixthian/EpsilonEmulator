namespace Epsilon.Launcher;

public sealed record RoomEntryProxyInput(string? Ticket, long RoomId, string? Password, bool SpectatorMode);
public sealed record RoomMoveProxyInput(string? Ticket, long RoomId, int DestinationX, int DestinationY);
public sealed record RoomChatProxyInput(string? Ticket, long RoomId, string Message);
