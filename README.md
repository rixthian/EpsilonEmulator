<p align="center">
  <img src="assets/epsilon-logo.png" alt="Epsilon Emulator logo" width="560" />
</p>

<h1 align="center">Epsilon Emulator</h1>

<p align="center">
  <strong>Modern hotel emulation focused on compatibility, security, and long-term maintainability.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-0.3.0--alpha.1-F4B400?style=for-the-badge" alt="Version badge" />
  <img src="https://img.shields.io/badge/runtime-.NET%2010-512BD4?style=for-the-badge" alt=".NET 10 badge" />
  <img src="https://img.shields.io/badge/compatibility-RELEASE63-111111?style=for-the-badge" alt="Compatibility badge" />
  <img src="https://img.shields.io/badge/status-active%20alpha-0F9D58?style=for-the-badge" alt="Alpha status badge" />
</p>

<p align="center">
  <a href="docs/architecture/overview.md">Architecture</a>
  ·
  <a href="docs/compatibility/target-client.md">Compatibility</a>
  ·
  <a href="docs/roadmap/phase-01.md">Roadmap</a>
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
| Version | `0.3.0-alpha.1` |
| Architecture | Modular monolith |
| Primary persistence | `PostgreSQL` |
| Cache / transient infra | `Redis` |
| Admin/API | `ASP.NET Core` |
| Protocol model | External packet and command manifests |
| Compatibility family | `RELEASE63` |

## Current Status

Epsilon is already beyond scaffold stage. The repository currently includes:

- protocol packet and command registries
- room entry state machine
- room runtime snapshots and mutable room interaction services
- bootstrap/session surfaces
- housekeeping and support slices
- package and asset import pipelines
- security hardening for current gameplay endpoints

Recent hardening already covered:

- forged in-room identity for movement and chat
- missing capability checks on room-affecting commands
- runtime concurrency issues in the in-memory room slice
- unsafe `sign` and `carry` payload handling
- missing flood control in room chat
- basic actor and furni collision checks

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
- [`docs/architecture/`](docs/architecture/) — architecture and domain design
- [`docs/compatibility/`](docs/compatibility/) — protocol and target-client material
- [`docs/decisions/`](docs/decisions/) — architectural decisions
- [`docs/roadmap/`](docs/roadmap/) — execution roadmap
- [`catalog/`](catalog/) — schemas and generated manifests
- [`tools/`](tools/) — importers and support tooling
- [`research/`](research/) — analysis only, never runtime dependency

## Key Documents

- [Architecture Overview](docs/architecture/overview.md)
- [Module Boundaries](docs/architecture/modules.md)
- [Design Principles](docs/architecture/design-principles.md)
- [Client Platform Strategy](docs/architecture/client-platform-strategy.md)
- [Hotel Domain Blueprint](docs/architecture/hotel-domain-blueprint.md)
- [Protocol Baseline](docs/compatibility/protocol-baseline.md)
- [Target Client](docs/compatibility/target-client.md)
- [Modern Runtime Decision](docs/decisions/0001-modern-runtime.md)
- [No Hardcoded Protocol And Content Rules](docs/decisions/0002-no-hardcoded-protocol-and-content-rules.md)

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
curl http://127.0.0.1:5000/health
```

## Roadmap

The next major milestones are:

1. complete authenticated session flow and remove development auth assumptions
2. wire more gameplay actions through the protocol layer
3. implement inventory and furni mutation with anti-duplication guarantees
4. expand persistence beyond the current read-heavy slices
5. continue package, public-room, icon, and visual asset pipelines

## Non-Goals

- cloning a specific legacy emulator architecture
- copying legacy SQL schemas into the core domain
- embedding compatibility rules directly inside gameplay code
- mixing research artifacts into production execution
- depending on Adobe Flash as a required runtime

## Disclaimer

Epsilon Emulator is an independent software project. It is not affiliated with,
endorsed by, or represented as an official Habbo or Sulake product.
