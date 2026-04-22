namespace Epsilon.Persistence;

internal sealed class RoomRuntimeShardState
{
    public RoomRuntimeShardState(long nextMessageId)
    {
        NextMessageId = nextMessageId;
    }

    public object SyncRoot { get; } = new();

    public long NextMessageId { get; set; }
}
