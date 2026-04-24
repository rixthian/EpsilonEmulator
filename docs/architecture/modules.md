# Module Boundaries

The cross-surface access boundary is defined in [platform-boundaries.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/platform-boundaries.md). Use that document when deciding whether a behavior belongs to the CMS, launcher app, game loader, launcher backend, runtime gateway, or emulator.

The local service orchestration boundary is defined in [local-runtime.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/local-runtime.md). Use that document when deciding how CMS, launcher, gateway, assets, imaging, database, Redis, and admin services should run together.

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

It should translate packet payloads into typed application commands that are then handled by the flow model defined in [client-entry-flow.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/client-entry-flow.md).

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
- catalog administration commands with validation, audit, and publication control

Must not:

- expose catalog mutation endpoints to normal users
- let staff tools write directly to catalog tables without command validation, audit, and publication control

## CMS base

Responsibilities:

- serve the public-facing CMS platform from the adopted local base in `vendor/cms-runtime-base`
- resolve web sessions against the hotel backend
- provide CMS-facing APIs for home, account, launcher access, and support surfaces
- hand users off to the launcher without pretending the CMS is the game

Must not:

- simulate rooms
- claim hotel presence
- replace launcher or client runtime logic

## `apps/epsilon-launcher-native`

Responsibilities:

- redeem one-time launcher access codes
- resolve desktop launcher config, channels, and launch profiles
- start the selected client package
- report launcher-side telemetry

Must not:

- simulate hotel state
- confirm that the user is inside the hotel
- replace the emulator as runtime authority

## `tools`

Responsibilities:

- clean generated local artifacts
- verify naming, hygiene, language scope, tests, builds, and local runtime health
- start and check the temporary CMS runtime base
- package launcher builds

Must not:

- act as gameplay runtime authority
- write directly into live hotel state
- become a second application layer
