# File Naming Policy

Date: 2026-04-21

## Rule

File names must be:

- explicit about responsibility
- short enough to scan quickly
- consistent with type names
- free of historical project vocabulary

## Preferred Pattern

Use:

- `Thing.cs`
- `IThingRepository.cs`
- `InMemoryThingRepository.cs`
- `PostgresThingRepository.cs`
- `ThingService.cs`
- `ThingSnapshot.cs`

Avoid redundant words when the domain type already carries them.

Examples:

- prefer `IVoucherRepository.cs` over `IVoucherDefinitionRepository.cs`
- prefer `PublicRoomPackageDefinition.cs` over `PublicRoomAssetPackageDefinition.cs`
- prefer `ICharacterPreferenceRepository.cs` over `ICharacterInterfacePreferenceRepository.cs`

## Boundary Rule

Do not put implementation detail, client era, or old project naming into file names unless it is a deliberate compatibility surface.

Allowed examples:

- `Release63PacketManifest.json`
- `PostgresRoomRepository.cs`

Disallowed examples:

- file names that mention old emulator projects
- vague suffix chains such as `DefinitionRepositoryManager`

## Practical Limit

Target file names should usually stay under 40 characters.

Crossing that limit is allowed only when shortening would make the role ambiguous.
