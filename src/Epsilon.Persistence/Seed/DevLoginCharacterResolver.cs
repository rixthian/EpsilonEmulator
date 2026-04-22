using Epsilon.Auth;
using Epsilon.CoreGame;

namespace Epsilon.Persistence;

/// <summary>
/// Resolves a login name to hotel identity values for the development authenticator.
/// Bridges Epsilon.Auth → Epsilon.CoreGame without creating a circular reference.
/// </summary>
internal sealed class DevLoginCharacterResolver : IDevLoginCharacterResolver
{
    private readonly ICharacterProfileRepository _characterProfileRepository;

    public DevLoginCharacterResolver(ICharacterProfileRepository characterProfileRepository)
    {
        _characterProfileRepository = characterProfileRepository;
    }

    public async ValueTask<DevLoginIdentity?> ResolveAsync(string loginName, CancellationToken cancellationToken = default)
    {
        CharacterProfile? profile = await _characterProfileRepository.GetByUsernameAsync(loginName, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        return new DevLoginIdentity(profile.AccountId.Value, profile.CharacterId.Value);
    }
}
