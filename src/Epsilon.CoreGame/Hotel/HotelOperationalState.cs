namespace Epsilon.CoreGame;

public sealed class HotelOperationalState : IHotelOperationalState
{
    // A single lock guarantees that IsLockdownActive and LockdownMessage are
    // always written and read as a consistent pair. Using two separate volatile
    // fields is insufficient on weakly-ordered architectures (ARM) because
    // there is no StoreStore barrier between consecutive volatile writes.
    private readonly object _sync = new();
    private bool _lockdownActive;
    private string? _lockdownMessage;

    public bool IsLockdownActive { get { lock (_sync) { return _lockdownActive; } } }
    public string? LockdownMessage { get { lock (_sync) { return _lockdownMessage; } } }

    public void ActivateLockdown(string? message)
    {
        lock (_sync)
        {
            _lockdownMessage = message;
            _lockdownActive = true;
        }
    }

    public void DeactivateLockdown()
    {
        lock (_sync)
        {
            _lockdownActive = false;
            _lockdownMessage = null;
        }
    }
}
