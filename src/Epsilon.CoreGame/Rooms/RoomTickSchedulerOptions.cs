namespace Epsilon.CoreGame;

public sealed class RoomTickSchedulerOptions
{
    public const string SectionName = "RoomTicks";

    public int TickIntervalMilliseconds { get; set; } = 410;

    public int RollerIntervalTicks { get; set; } = 6;
}
