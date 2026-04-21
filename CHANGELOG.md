# Changelog

All notable changes to Epsilon Emulator should be documented in this file.

The format is intentionally simple while the project is still in alpha.

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
