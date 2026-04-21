namespace Epsilon.Auth;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}

