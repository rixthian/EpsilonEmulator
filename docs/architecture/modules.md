# Module Boundaries

## `Epsilon.Gateway`

Responsibilities:

- accept TCP connections
- manage session lifecycle
- read and write packet frames
- rate limit, log, and disconnect abusive clients

Must not:

- execute business rules directly
- query storage directly for gameplay behavior

## `Epsilon.Protocol`

Responsibilities:

- define packet ids and packet contracts
- decode client packets into internal commands
- encode outbound events into version-specific payloads
- host protocol fixtures and compatibility notes

Must not:

- own gameplay state
- talk directly to the database

It should translate packet payloads into typed application commands that are then handled by the flow model defined in [client-hotel-flow-blueprint.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/client-hotel-flow-blueprint.md).

## `Epsilon.Auth`

Responsibilities:

- account login and session issuance
- SSO or ticket flows
- ban and access checks
- identity/session audit logging

## `Epsilon.CoreGame`

Responsibilities:

- shared application services
- user profile operations
- session bootstrap composition
- wallet, badge, achievement, and messenger read models
- command availability and other hotel-wide gameplay primitives

## `Epsilon.Rooms`

Responsibilities:

- room state
- actor positions
- chat and room presence
- item placement
- room-scoped minigame state
- deterministic simulation ticks

This module is the heart of gameplay correctness and should stay highly testable.

## `Epsilon.Content`

Responsibilities:

- furnidata
- product data
- texts and variables
- figure data
- badge/effect metadata
- room visual asset manifests

## `Epsilon.Persistence`

Responsibilities:

- PostgreSQL repositories
- migrations
- audit/event storage
- import pipelines from legacy datasets

## `Epsilon.AdminApi`

Responsibilities:

- moderation endpoints
- operational health endpoints
- replay/test tooling endpoints
- content and compatibility inspection tools

## `tools/importers`

Responsibilities:

- convert legacy asset packages into canonical Epsilon manifests
- inventory public-room bundles and shared assets
- normalize filenames and metadata
- separate extraction concerns from runtime loading concerns
