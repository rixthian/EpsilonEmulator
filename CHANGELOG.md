# Changelog

All notable changes to Epsilon Emulator should be documented in this file.

The format is intentionally simple while the project is still in alpha.

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
