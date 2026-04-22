namespace Epsilon.Auth;

/// <summary>
/// Resolves a login name to hotel identity values (account + character ids).
/// Implemented in the persistence layer so Epsilon.Auth does not need a
/// direct reference to Epsilon.CoreGame.
/// </summary>
public interface IDevLoginCharacterResolver
{
    ValueTask<DevLoginIdentity?> ResolveAsync(string loginName, CancellationToken cancellationToken = default);
}

public sealed record DevLoginIdentity(long AccountId, long CharacterId);
