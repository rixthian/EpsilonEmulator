namespace Epsilon.CoreGame;

public sealed class HotelOperationalState : IHotelOperationalState
{
    // volatile ensures cross-thread visibility without a full lock on the read path.
    private volatile bool _lockdownActive;
    private volatile string? _lockdownMessage;

    public bool IsLockdownActive => _lockdownActive;
    public string? LockdownMessage => _lockdownMessage;

    public void ActivateLockdown(string? message)
    {
        _lockdownMessage = message;
        _lockdownActive = true;
    }

    public void DeactivateLockdown()
    {
        _lockdownActive = false;
        _lockdownMessage = null;
    }
}
