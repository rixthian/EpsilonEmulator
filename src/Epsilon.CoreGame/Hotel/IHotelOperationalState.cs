namespace Epsilon.CoreGame;

/// <summary>
/// Shared singleton for hotel-wide emergency operational flags.
/// Checked by room entry and session creation to block new activity during
/// lockdowns or maintenance windows.
/// </summary>
public interface IHotelOperationalState
{
    bool IsLockdownActive { get; }
    string? LockdownMessage { get; }

    void ActivateLockdown(string? message);
    void DeactivateLockdown();
}
