using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed class HousekeepingSnapshotService : IHousekeepingSnapshotService
{
    private readonly ICharacterProfileRepository _characterProfileRepository;
    private readonly IRoleAccessRepository _roleAccessRepository;
    private readonly IChatCommandRepository _chatCommandRepository;
    private readonly IHotelAdvertisementRepository _hotelAdvertisementRepository;
    private readonly IClientPackageRepository _clientPackageRepository;

    public HousekeepingSnapshotService(
        ICharacterProfileRepository characterProfileRepository,
        IRoleAccessRepository roleAccessRepository,
        IChatCommandRepository chatCommandRepository,
        IHotelAdvertisementRepository hotelAdvertisementRepository,
        IClientPackageRepository clientPackageRepository)
    {
        _characterProfileRepository = characterProfileRepository;
        _roleAccessRepository = roleAccessRepository;
        _chatCommandRepository = chatCommandRepository;
        _hotelAdvertisementRepository = hotelAdvertisementRepository;
        _clientPackageRepository = clientPackageRepository;
    }

    public async ValueTask<HousekeepingSnapshot?> BuildAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterProfile? character = await _characterProfileRepository.GetByIdAsync(characterId, cancellationToken);

        if (character is null)
        {
            return null;
        }

        IReadOnlyList<StaffRoleDefinition> roleDefinitions =
            await _roleAccessRepository.GetRoleDefinitionsAsync(cancellationToken);
        IReadOnlyList<AccessCapability> capabilityCatalog =
            await _roleAccessRepository.GetCapabilityCatalogAsync(cancellationToken);
        IReadOnlyList<StaffRoleAssignment> assignments =
            await _roleAccessRepository.GetAssignmentsByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<ChatCommandDefinition> commands =
            await _chatCommandRepository.GetAvailableByCharacterIdAsync(characterId, cancellationToken);
        IReadOnlyList<HotelAdvertisement> advertisements =
            await _hotelAdvertisementRepository.GetActiveByPlacementAsync("landing", cancellationToken);
        IReadOnlyList<ClientPackageManifest> clientPackages =
            await _clientPackageRepository.GetAllAsync(cancellationToken);

        IReadOnlyList<StaffRoleDefinition> activeRoles = roleDefinitions
            .Where(role => assignments.Any(assignment => string.Equals(assignment.RoleKey, role.RoleKey, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(role => role.RankLevel)
            .ToArray();

        HashSet<string> capabilityKeys = new(StringComparer.OrdinalIgnoreCase);
        foreach (StaffRoleDefinition role in activeRoles)
        {
            foreach (string capabilityKey in role.CapabilityKeys)
            {
                capabilityKeys.Add(capabilityKey);
            }
        }

        IReadOnlyList<AccessCapability> effectiveCapabilities = capabilityCatalog
            .Where(capability => capabilityKeys.Contains(capability.CapabilityKey))
            .OrderBy(capability => capability.CapabilityKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HousekeepingSnapshot(
            character,
            activeRoles,
            effectiveCapabilities,
            commands,
            advertisements.Where(advertisement => advertisement.HasRemainingCapacity).ToArray(),
            clientPackages);
    }
}
