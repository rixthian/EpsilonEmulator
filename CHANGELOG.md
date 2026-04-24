# Changelog

All notable changes to Epsilon Emulator should be documented in this file.

The format is intentionally simple while the project is still in alpha.

## [0.4.0-alpha.5] - 2026-04-23

### Added

- CMS backend surface for:
  - authenticated session lookup
  - public home payload
  - launcher access payload
  - launcher access code generation
- public CMS portal pages for:
  - homepage
  - login
  - register
  - authenticated launcher selection
- launcher desktop contract:
  - `GET /launcher/desktop/config`
  - `GET /launcher/update/channels`
  - `GET /launcher/update/channel/{channelKey}`
  - `GET /launcher/launch-profiles`
  - `POST /launcher/launch-profiles/select`
  - `POST /launcher/client-started`
- one-time launcher access-code issue/redeem flow and launcher telemetry tracking
- published `game-loader` loader asset under the launcher-controlled asset root
- native desktop launcher app under `apps/epsilon-launcher-native`
- reference Electron launcher shell under `apps/epsilon-launcher-desktop`
- macOS launcher packaging pipeline producing:
  - `EpsilonLauncher.app`
  - `EpsilonLauncher-macOS-arm64.dmg`
- room tick scheduler, room animation support, and first roller runtime service
- collectibles platform slice including:
  - wallet challenge/link groundwork
  - collector profile and level
  - emerald accrual and ledgering
  - gift boxes
  - factories
  - CollectiMatic
  - marketplace listing/buy flow

### Changed

- CMS copy and structure now present as a game portal instead of a system/debug-first access page
- launcher flow now enforces the correct product rule:
  - CMS authenticates
  - launcher/app starts the client
  - emulator confirms actual hotel presence
- `/launcher/play` now redirects back to the loader instead of pretending to be the hotel client
- launcher loader no longer exposes end-user runtime/debug assumptions as product UI
- launcher native packaging now reads the application version from `Directory.Build.props`
- documentation now describes the CMS/backend/app/launcher split, current instability, and release state more explicitly

### Known Issues

- CMS and launcher app surfaces are functional but still unstable
- final Unity/Nitro desktop package targets are not published yet
- `game-loader` remains a provisional loader surface until the final Unity/Nitro desktop package is published
- live durability is still limited by remaining `InMemory` dependencies
- no production-grade release signing/notarization is in place for the macOS launcher package

## [0.4.0-alpha.4] - 2026-04-23

### Security

- replaced non-atomic read-check-write pattern in catalog purchases with a single `TryApplyDebitsAsync` call, eliminating the double-spend race condition
- replaced non-atomic emerald spend/grant in `EmeraldLedgerService` with atomic wallet repository methods
- replaced non-atomic marketplace buy with `TryDeactivateListingAsync` — listing is claimed atomically before funds are spent, preventing double-buy
- replaced non-atomic `AccrueEmeraldsAsync` 20-hour gate with `TryAdvanceAccrualTimestampAsync`, preventing duplicate daily grants under concurrent requests
- replaced non-atomic factory claim with `TryClaimFactoryAsync`, preventing duplicate reward collection under concurrent requests
- replaced non-atomic collectible ownership mutations (`AddCollectiblesAsync`, `RemoveCollectiblesAsync`) with `AddKeysAsync` / `TryRemoveKeysAsync`, eliminating lost-write races
- replaced non-atomic daily respect spend in `GiveRespectAsync` with `TrySpendDailyRespectAsync`, preventing over-spending under concurrent commands
- fixed memory ordering in `HotelOperationalState`: replaced dual `volatile` fields with a `lock` so `LockdownMessage` and `IsLockdownActive` are always read and written as a consistent pair
- added `lock (_sync)` to all three unguarded read methods in `InMemoryCharacterProfileRepository`, closing read-write races against concurrent `StoreAsync` / `CreateAsync`
- added `lock (_syncRoot)` to all methods in `InMemoryRoomItemRepository` and changed `GetByRoomIdAsync` to return a snapshot instead of the live list reference
- added `_storeKeysLock` to `InMemoryRoomRuntimeRepository` to guard dictionary key enumeration and mutation across shards
- dev-only collectible ownership and XP grant endpoints are now registered only in the `Development` environment
- `/hotel/bootstrap/{characterId}` and `/hotel/sessions/{characterId}` now require the caller's own session ticket and reject cross-character reads
- `/hotel/rooms/{roomId}/runtime` now requires an authenticated session
- fixed `PickAllAsync` to credit room items to the room **owner**, not the moderator who issued the command

### Changed

- `ApplyCreditAsync` added to `IWalletRepository` for atomic credit operations
- `TryApplyDebitsAsync` added to `IWalletRepository` for atomic multi-currency debit operations
- `TryDeactivateListingAsync` added to `ICollectStateRepo` for atomic marketplace listing claim
- `TryAdvanceAccrualTimestampAsync` added to `ICollectorProgressRepo`
- `TryClaimFactoryAsync` added to `ICollectStateRepo`
- `AddKeysAsync` / `TryRemoveKeysAsync` added to `ICollectibleOwnershipRepository`
- `TrySpendDailyRespectAsync` added to `ICharacterProfileRepository`; Postgres implementation uses `UPDATE ... WHERE daily_respect_points > 0 RETURNING *` for server-side atomicity
- `FindRoomForActorAsync` added to `IRoomRuntimeRepository`; replaces O(n×m) fan-out in `ResolveCurrentRoomIdAsync` with a single O(n) scan
- `CountWithOwnedCollectiblesAsync` added to `ICollectibleOwnershipRepository`; replaces hardcoded 1–500 loop in `CountCollectorsAsync` with a single store scan
- `BroadcastHotelAlertAsync`, `KickAllAsync`, and `ActivateMaintenanceAsync` now fan out per-room operations with `Task.WhenAll` instead of sequential awaits
- `ClaimFactoryAsync` now validates the required category directly before the atomic claim, removing the redundant `GetFactoriesAsync` round-trip
- `GetPublicSnapshotAsync` now fetches market listings once instead of twice, and the dead empty `foreach` loop is removed
- bot actor ID collision detection added in `EnsureRoomBotsAsync`: duplicate hashes within the same batch are resolved by applying a deterministic offset

## [0.4.0-alpha.3] - 2026-04-22

### Added

- central `configuration/` surface with shared, gateway, launcher, admin, and feature templates
- platform roadmap describing the multi-version adapter strategy and long-term roleplay/product direction
- feature-level progress matrix in the `README`
- service/domain-oriented source layout across gateway, auth, core game, protocol, games, rooms, persistence, launcher, and tests
- client/build/content ingest schemas and manifests for launcher, avatar, figure, and client-root inventories

### Changed

- repository structure is now organized by service responsibility instead of large flat project roots
- `README` now documents mission, compatibility strategy, repository structure, module coverage, and feature coverage
- compatibility documentation now frames `RELEASE63` as the first strong baseline rather than the final scope
- configuration documentation now explains layered overrides and long-term platform scaling

### Hygiene

- removed low-value legacy/source-history documentation from the runtime-facing repo surface
- normalized staged file moves into tracked renames
- repository documentation now matches the actual code structure
- retained passing build and hygiene checks after the reorganization

## [0.4.0-alpha.2] - 2026-04-22

### Added

- rank-derived command catalog covering regular users through owner/staff tiers
- classic room/runtime commands including `:chooser`, `:furni`, `:whisper`, and `:shout`
- moderation command slice for room-present users:
  - `:alert`
  - `:kick`
  - `:softkick`
  - `:shutup`
  - `:unmute`
  - `:ban`
  - `:superban`
  - `:transfer`
- in-memory moderation repository with active ban records
- chat normalization coverage for classic extended-symbol input

### Changed

- room entry now registers actor presence in runtime instead of returning only a snapshot
- `:pickall` now returns room items to inventory
- command availability is derived from rank scope and capabilities instead of one seeded character
- room/runtime command execution now includes moderation, room inspection, and directed chat paths

### Security

- active bans now block future room entry
- room-target moderation actions operate only on present, resolved actors
- chat input now strips control characters while preserving valid extended symbols
- timed room mute enforcement blocks muted actors from sending room chat

## [0.3.0-alpha.1] - 2026-04-21

### Added

- centralized package version management for the `.NET 10` toolchain
- protocol and gateway runtime now report an explicit application version
- GitHub-ready project presentation with branded `README` content
- repository logo asset under `assets/epsilon-logo.png`

### Changed

- runtime upgraded and aligned to `.NET 10`
- test project moved to `xUnit v3`
- `README` rewritten for public GitHub presentation and clearer project positioning

### Security

- mutable hotel actions now bind `CharacterId` from the server-side session ticket
- room command execution now re-checks required capabilities for sensitive commands
- room chat now enforces flood control windows
- `sign` and `carry` inputs are normalized and constrained
- basic actor and non-walkable furni collision checks added to movement validation
- in-memory room runtime now serializes mutations to avoid concurrency corruption
