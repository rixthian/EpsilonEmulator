using Epsilon.Content;

namespace Epsilon.CoreGame;

public sealed record HousekeepingSnapshot(
    CharacterProfile Character,
    IReadOnlyList<StaffRoleDefinition> ActiveRoles,
    IReadOnlyList<AccessCapability> EffectiveCapabilities,
    IReadOnlyList<ChatCommandDefinition> AvailableCommands,
    IReadOnlyList<HotelAdvertisement> ActiveLandingAdvertisements,
    IReadOnlyList<ClientPackageManifest> ClientPackages);
