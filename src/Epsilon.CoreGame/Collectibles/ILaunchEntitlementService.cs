namespace Epsilon.CoreGame;

public interface ILaunchEntitlementService
{
    ValueTask<LaunchEntitlementSnapshot> EvaluateAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default);
}
