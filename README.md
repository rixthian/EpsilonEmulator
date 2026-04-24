<p align="center">
  <img src="assets/epsilon-logo.png" alt="Epsilon Emulator logo" width="560" />
</p>

<h1 align="center">Epsilon Emulator</h1>

<p align="center">
  <strong>Modern hotel emulation focused on compatibility, security, and long-term maintainability.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-0.4.0--alpha.5-F4B400?style=for-the-badge" alt="Version badge" />
  <img src="https://img.shields.io/badge/runtime-.NET%2010-512BD4?style=for-the-badge" alt=".NET 10 badge" />
  <img src="https://img.shields.io/badge/compatibility-RELEASE63-111111?style=for-the-badge" alt="Compatibility badge" />
  <img src="https://img.shields.io/badge/status-active%20alpha-0F9D58?style=for-the-badge" alt="Alpha status badge" />
</p>

<p align="center">
  <a href="docs/architecture/overview.md">Architecture</a>
  ·
  <a href="docs/compatibility/target-client.md">Compatibility</a>
  ·
  <a href="docs/architecture/update-automation-brain.md">Update Brain</a>
  ·
  <a href="CHANGELOG.md">Changelog</a>
  ·
  <a href="docs/roadmap/platform-roadmap.md">Roadmap</a>
  ·
  <a href="docs/decisions/0001-modern-runtime.md">Decisions</a>
</p>

---

## Overview

Epsilon Emulator is a clean-room hotel emulator project built to preserve classic
client behavior while replacing fragile legacy runtime assumptions with a modern,
versioned, testable architecture.

The target is not an old runtime. The target is the hotel contract:

- protocol behavior
- room simulation rules
- item and catalog behavior
- social flows
- moderation flows
- content loading rules

## Why Epsilon Exists

Older emulator projects proved that the game could be revived, but they also
repeated the same problems:

- hardcoded packets and content metadata
- weak authentication and authorization boundaries
- fragile in-memory runtime state
- database schema treated as domain design
- CMS, launcher, admin, and hotel runtime mixed together

Epsilon exists to fix those problems directly.

## Mission

Epsilon is being built as a multi-era hotel platform.

Long term, the project should be able to:

- run multiple Habbo compatibility families through explicit adapters
- preserve classic hotel behavior without inheriting legacy emulator structure
- add modern infrastructure, security, observability, and deployment discipline
- support new original features such as roleplay systems, modern social features, and future client surfaces

The mission is not “one retro clone per version.”
The mission is one stable hotel platform with:

- version-aware protocol adapters
- version-aware content visibility
- stable core hotel domains
- room for original product evolution

## Design Pillars

| Pillar | Meaning |
|---|---|
| Compatibility-first | The emulator preserves hotel behavior, not legacy code structure. |
| Modern runtime | `.NET 10`, explicit module boundaries, typed configuration, centralized package management. |
| Data-driven rules | Protocol and content behavior should live in manifests, pipelines, and importable definitions. |
| Security-aware | Gameplay correctness and trust boundaries are treated as first-class concerns. |
| Long-term survivability | The project is designed to remain maintainable across years, not just to boot quickly. |

## Current Technical Direction

| Area | Choice |
|---|---|
| Runtime | `.NET 10` |
| Version | `0.4.0-alpha.5` |
| Architecture | Modular monolith |
| Primary persistence | `PostgreSQL` |
| Cache / transient infra | `Redis` |
| Admin/API | `ASP.NET Core` |
| Protocol model | External packet and command manifests |
| Compatibility family | `RELEASE63` |

## Compatibility Strategy

Epsilon starts with one strong baseline and expands outward.

- first stable adapter target: `RELEASE63`
- long-term target: multi-version support across classic, Flash-era, and later compatible hotel surfaces
- expansion rule: each client family gets its own adapter layer instead of leaking revision logic into hotel services

That means:

- `Epsilon.CoreGame`, `Epsilon.Rooms`, `Epsilon.Content`, and `Epsilon.Games` stay mostly stable
- compatibility-specific behavior is isolated in protocol, launcher, content, and rendering adapters
- new technology can be added without breaking old hotel contracts

## Current Status

Epsilon is already beyond scaffold stage. The repository currently includes:

- protocol packet and command registries
- room entry state machine
- room runtime snapshots and mutable room interaction services
- bootstrap/session surfaces
- housekeeping and support slices
- package and asset import pipelines
- security hardening for current gameplay endpoints

Current release focus in `0.4.0-alpha.5`:

- public-facing CMS portal with homepage, login, register, launcher choice, and cleaner hotel-facing copy
- CMS backend + launcher handoff with one-time launcher access codes
- native desktop launcher source integrated into the repo and packaged as a macOS `.dmg`
- desktop launcher contract for local config, update channels, launch profiles, and `client-started` telemetry
- published `game-loader` entry routed through the launcher instead of the CMS
- first room tick scheduler, room animation slice, and roller runtime support
- collector platform slice with wallet/link groundwork, progression, emerald accrual, factory/gift/recycle/market loops

Recent hardening already covered:

- forged in-room identity for movement and chat
- missing capability checks on room-affecting commands
- runtime concurrency issues in the in-memory room slice
- unsafe `sign` and `carry` payload handling
- missing flood control in room chat
- basic actor and furni collision checks

Recent runtime expansion now covered:

- `:pickall` returns room items to inventory
- room presence migrates cleanly when an actor enters another room
- moderation commands now cover kick, mute, ban, transfer, and alert paths
- room inspection and directed chat commands now exist in the runtime layer

## Development Progress

The percentages below are current engineering estimates of implemented, verified,
and operable coverage. They are intended to show actual module maturity, not
issue-count vanity metrics.

| Scope | Progress | Notes |
|---|---:|---|
| Global emulator operativity | `98%` | The project now spans gameplay, CMS, launcher, and native app surfaces, but public-facing stability is still not production grade. |
| Architecture quality | `99%` | Core boundaries between CMS, launcher, client, and emulator are now explicit and defensible. |
| Repository hygiene | `99%` | Naming, staging discipline, and runtime separation are in strong condition. |
| `Epsilon.Gateway` | `90%` | Main runtime/API surface is broad and stable, but protocol parity is still incomplete. |
| `Epsilon.Protocol` | `54%` | Packet and command manifests exist, but live gameplay still trails the HTTP/runtime path. |
| `Epsilon.Auth` | `86%` | Session/auth boundaries and modern password hashing are in place; full production auth migration is pending. |
| `Epsilon.CoreGame` | `91%` | Hotel flows, commands, moderation, commerce, room entry, and snapshots are heavily developed. |
| `Epsilon.Rooms` | `82%` | Room definitions, runtime interaction, and presence handling are solid; deeper furni mutation is still incomplete. |
| `Epsilon.Content` | `91%` | Catalog, badges, avatar content, campaign/content modeling, and import structures are advanced. |
| `Epsilon.Persistence` | `58%` | Architecture is strong, but too much live behavior still depends on `InMemory`. |
| `Epsilon.Games` | `75%` | Game definitions and BattleBall lifecycle exist, but full live loops are still missing. |
| `Epsilon.Launcher` | `90%` | Client bootstrap, access-code flow, channels, profiles, telemetry, and published client routing are now in place. |
| `Epsilon.AdminApi` | `74%` | Admin/runtime inspection works, but full moderation and operational tooling is not complete. |
| CMS platform | `68%` | CMS backend and public portal now exist, but the surface is still unstable and not yet hardened for real production traffic. |
| Desktop launcher apps | `74%` | Native Avalonia launcher and Electron reference shell exist; final Unity/Nitro launch targets are still pending. |
| Asset/content ingest pipeline | `94%` | Client roots, builds, avatar bundles, figures, badges, and related manifests are organized and reproducible. |
| Multi-process runtime integrity | `84%` | Shared sessions improved, but shared room presence/state still needs stronger distributed handling. |

### Feature Coverage

| Feature area | Progress | Notes |
|---|---:|---|
| Authentication and session lifecycle | `87%` | Modern password hashing, ticket/session stores, heartbeats, and disconnect flow exist; production auth migration is still pending. |
| Protocol-driven hotel execution | `54%` | Protocol manifests and command execution bridge exist, but too much live behavior still runs through HTTP-first paths. |
| Room runtime and mobility | `82%` | Entry, presence, chat, directed commands, and adjacency movement are in place; full path queues and rich furni mutation are still incomplete. |
| Chat and command system | `93%` | Rank-aware commands, moderation commands, whisper/shout, link handling, and normalization are working. |
| Moderation and staff tooling | `78%` | Room-present moderation is real, but offline/account-wide workflows and case tooling are still missing. |
| Bot runtime | `69%` | Deterministic runtime bots, scripted replies, and service actions exist; pathing, scheduling, and admin configuration still need expansion. |
| Groups and social foundations | `72%` | Group creation, membership, and linked private rooms work; forums and richer role management remain incomplete. |
| Catalog, economy, and inventory | `88%` | Catalog content, purchases, vouchers, and inventory slices are advanced; full trading-grade mutation is still not finished. |
| Avatar, badges, and collectible content | `92%` | Badge catalog, avatar asset digestion, figure manifests, and collectible-ready content models are in strong shape. |
| Public rooms and hotel world features | `85%` | Public-room definitions, behaviors, bots, and package inventories are strong; more interactive venue logic is still needed. |
| Games | `75%` | BattleBall lifecycle and game session foundations are in place; deeper live loops for all game families are still pending. |
| Launcher and connection policy | `90%` | Client profiles, launcher access codes, desktop launcher contracts, and device-aware connection policies are now in place. |
| CMS, launcher app, and access flow | `70%` | Access logic is now correct, but the CMS and launcher app both need another hardening pass before they can be called stable. |
| Configuration platform | `90%` | Root configuration layering and service templates are in place; more feature sections need to be fully bound into runtime. |
| Multi-version compatibility foundation | `63%` | The adapter strategy, client/build manifests, and launcher profiles are in place; only `RELEASE63` is currently the strong baseline. |
| Roleplay and original Epsilon features | `22%` | The platform direction is defined, but dedicated roleplay systems are still future work. |

Current weakest points, in order:

1. `Epsilon.Persistence` still relies too heavily on `InMemory` for live state and CMS continuity.
2. `Epsilon.Protocol` still trails the HTTP/runtime path for real gameplay execution.
3. CMS and launcher app surfaces are functional but still unstable.
4. `Epsilon.Games` still needs deeper live round loops beyond lifecycle control.
5. `Epsilon.Rooms` still needs full furni placement, pickup, and trading-grade mutation paths.

## Release Snapshot

`0.4.0-alpha.5` is the first release where the public web, launcher, and native app are treated as product surfaces instead of debug shells.

Delivered in this release:

- CMS:
  - homepage
  - login/register
  - authenticated launcher access choice
  - launcher code generation
- launcher backend:
  - desktop config
  - update channels
  - launch profiles
  - client-started telemetry
- native launcher packaging:
  - Avalonia/VB launcher imported and adapted
  - macOS `.app`
  - macOS `.dmg`
- access rule:
  - CMS never claims hotel presence
  - launcher never claims hotel presence
  - only emulator-confirmed runtime presence counts as real entry

Known instability in this release:

- CMS presentation and access flow were rebuilt quickly and still need hardening
- launcher native packaging exists, but Unity/Nitro package targets are still not published
- `game-loader` is provisional and does not represent the final social/isometric client
- production durability is still blocked by remaining `InMemory` slices

## Architecture Shape

```text
Gateway
  -> Protocol
  -> Auth
  -> CoreGame
  -> Rooms
  -> Content
  -> Persistence
  -> AdminApi
```

Each module has a narrow responsibility:

- `Epsilon.Gateway` handles HTTP/runtime entry surfaces
- `Epsilon.Protocol` owns compatibility manifests and command mapping
- `Epsilon.Auth` owns tickets, sessions, and authentication boundaries
- `Epsilon.CoreGame` owns hotel flows and gameplay services
- `Epsilon.Rooms` owns room and layout definitions
- `Epsilon.Content` owns catalog, item, package, and asset definitions
- `Epsilon.Persistence` owns replaceable storage providers
- `Epsilon.AdminApi` owns administrative surfaces

## Repository Map

- [`src/`](src/) — production code
- [`tests/`](tests/) — automated tests
- [`apps/`](apps/) — desktop launcher app sources
- [`cms/`](cms/) — CMS platform and preserved web surfaces
- [`configuration/`](configuration/) — shared and per-service configuration templates
- [`docs/architecture/`](docs/architecture/) — architecture and domain design
- [`docs/requirements/`](docs/requirements/) — local SDK and project execution requirements
- [`docs/releases/`](docs/releases/) — release snapshots and alpha release notes
- [`docs/compatibility/`](docs/compatibility/) — protocol and target-client material
- [`docs/decisions/`](docs/decisions/) — architectural decisions
- [`docs/roadmap/`](docs/roadmap/) — execution roadmap
- [`catalog/`](catalog/) — schemas and generated manifests
- [`tools/brain/`](tools/brain/) — update-intelligence brain for source watching, diffs, and SWF toolchain policy
- [`tools/`](tools/) — importers and support tooling
- [`references/`](references/) — source handling rules and reference hygiene

## Key Documents

- [Architecture Overview](docs/architecture/overview.md)
- [Module Boundaries](docs/architecture/modules.md)
- [CMS Platform](docs/architecture/cms-platform.md)
- [Launcher App Access](docs/architecture/launcher-app-access.md)
- [Desktop Launcher Spec](docs/architecture/desktop-launcher-spec.md)
- [Project Requirements](docs/requirements/project-requirements.md)
- [Launcher Popup To Game Loader Flow](docs/architecture/launcher-popup-loader-flow.md)
- [Local Orchestration Topology](docs/architecture/local-orchestration-topology.md)
- [Nitro Docker Reference](docs/reference-sources/nitro-docker.md)
- [Design Principles](docs/architecture/design-principles.md)
- [Client Platform Strategy](docs/architecture/client-platform-strategy.md)
- [Hotel Domain Blueprint](docs/architecture/hotel-domain-blueprint.md)
- [Protocol Baseline](docs/compatibility/protocol-baseline.md)
- [Target Client](docs/compatibility/target-client.md)
- [Modern Runtime Decision](docs/decisions/0001-modern-runtime.md)
- [No Hardcoded Protocol And Content Rules](docs/decisions/0002-no-hardcoded-protocol-and-content-rules.md)

## Code Structure

The source tree is now organized by service and domain so each project is easier
to scan and maintain.

- [`src/Epsilon.Auth/`](src/Epsilon.Auth/) — `Abstractions`, `Configuration`, `Contracts`, `Services`, `Storage`, `Startup`
- [`src/Epsilon.Content/`](src/Epsilon.Content/) — `Badges`, `Catalog`, `Client`, `Collectibles`, `Effects`, `Interfaces`, `Items`, `Localization`, `Navigator`, `Pets`, `PublicRooms`, `Vouchers`
- [`src/Epsilon.CoreGame/`](src/Epsilon.CoreGame/) — `Access`, `Accounts`, `Badges`, `Bots`, `Chat`, `Commerce`, `Groups`, `Hotel`, `Interface`, `Inventory`, `Moderation`, `Navigator`, `Packets`, `Pets`, `Roles`, `Rooms`, `Subscriptions`, `Support`, `Wallet`
- [`src/Epsilon.Games/`](src/Epsilon.Games/) — `BattleBall`, `SnowStorm`, `WobbleSquabble`, `Core`, `Runtime`, `Sessions`, `Startup`
- [`src/Epsilon.Gateway/`](src/Epsilon.Gateway/) — `Configuration`, `Console`, `Contracts`, `Startup`
- [`src/Epsilon.Launcher/`](src/Epsilon.Launcher/) — `Configuration`, `Models`, `Services`, `Startup`
- [`src/Epsilon.Persistence/`](src/Epsilon.Persistence/) — `Configuration`, `InMemory`, `Postgres`, `Redis`, `Runtime`, `Seed`, `Startup`
- [`src/Epsilon.Protocol/`](src/Epsilon.Protocol/) — `Configuration`, `Manifests`, `Registry`, `SelfCheck`, `Startup`
- [`src/Epsilon.Rooms/`](src/Epsilon.Rooms/) — `Contracts`, `Models`

The test tree follows the same approach:

- [`tests/Epsilon.CoreGame.Tests/`](tests/Epsilon.CoreGame.Tests/) — grouped into `Auth`, `Badges`, `Chat`, `Commands`, `Commerce`, `Games`, `Groups`, `Hotel`, `Moderation`, `Rooms`, `Runtime`

## Ethics

Epsilon is a preservation and interoperability project.

This repository does **not** aim to:

- redistribute proprietary assets without review
- normalize cracked or malware-risk archive material
- present low-trust sources as production dependencies
- misrepresent affiliation with Habbo or Sulake

Reference material may inform behavior and formats, but it must not become runtime
inheritance by accident.

## Security Position

Epsilon is still alpha software. It compiles, runs, and already exposes real gameplay
services, but the security surface is still being tightened as more game behavior is added.

Near-term priorities include:

- replacing development-only authentication
- reducing diagnostic exposure in production
- binding every mutable action to authenticated session state
- moving more mutable runtime state away from in-memory storage
- enforcing authorization consistently across gateway and services

## Local Development

### Requirements

- `.NET SDK 10`
- Docker or Docker Desktop

### Infrastructure

The repository includes a local development stack in [`compose.yaml`](compose.yaml):

- PostgreSQL `16`
- Redis `7`

Environment defaults live in [`.env.example`](.env.example).

### Quick Start

```bash
cp .env.example .env
docker compose up -d
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Epsilon.Gateway/Epsilon.Gateway.csproj
```

### Health Check

```bash
curl http://127.0.0.1:5100/health
```

## Roadmap

The next major milestones are:

1. complete authenticated session flow and remove development auth assumptions
2. wire more gameplay actions through the protocol layer
3. implement inventory and furni mutation with anti-duplication guarantees
4. expand persistence beyond the current read-heavy slices
5. continue package, public-room, icon, and visual asset pipelines
6. add compatibility adapters beyond `RELEASE63`
7. add original hotel extensions, including roleplay-oriented systems, without polluting compatibility layers

## Non-Goals

- cloning a specific legacy emulator architecture
- copying legacy SQL schemas into the core domain
- embedding compatibility rules directly inside gameplay code
- mixing research artifacts into production execution
- depending on Adobe Flash as a required runtime

## Disclaimer

Epsilon Emulator is an independent software project. It is not affiliated with,
endorsed by, or represented as an official Habbo or Sulake product.
