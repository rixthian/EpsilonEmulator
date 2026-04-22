# Changelog

All notable changes to Epsilon Emulator should be documented in this file.

The format is intentionally simple while the project is still in alpha.

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
