namespace Epsilon.CoreGame;

public interface IAccessControlService
{
    ValueTask<bool> HasCapabilityAsync(
        CharacterId characterId,
        string capabilityKey,
        CancellationToken cancellationToken = default);
}
