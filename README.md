<p align="center">
  <img src="assets/epsilon-logo.png" alt="Epsilon Emulator logo" width="520" />
</p>

<h1 align="center">Epsilon Emulator</h1>

<p align="center">
  Modern, compatibility-first hotel emulation focused on long-term maintainability,
  clean architecture, and ethical preservation.
</p>

<p align="center">
  <strong>Version:</strong> <code>0.3.0-alpha.1</code>
  <strong>&nbsp;•&nbsp;</strong>
  <strong>Runtime:</strong> <code>.NET 10</code>
  <strong>&nbsp;•&nbsp;</strong>
  <strong>Target Family:</strong> <code>RELEASE63</code>
</p>

## What Epsilon Is

Epsilon Emulator is a clean-room hotel emulator project designed to preserve classic
client behavior without inheriting the architectural debt of legacy emulators.

The compatibility target is the hotel contract:

- protocol behavior
- room simulation rules
- item and catalog behavior
- social and moderation flows
- content loading rules

The runtime is intentionally modern:

- versioned compatibility adapters
- modular monolith boundaries
- manifest-driven protocol and content rules
- replaceable infrastructure
- testable gameplay services

## Why This Project Exists

Most older emulator projects proved that the game could be revived, but they also
showed recurring problems:

- hardcoded packet and content rules
- fragile threading and runtime state
- weak authentication and admin boundaries
- mixed CMS/runtime responsibilities
- database schemas treated as application design

Epsilon exists to solve those problems directly.

## Principles

- Legacy emulators are research input, not base code.
- Compatibility rules must be explicit and versioned.
- Runtime code must remain independent from archive material.
- Security boundaries matter as much as gameplay correctness.
- Ten-year survivability matters more than short-term imitation.
- Uncertainty should be documented instead of guessed.

## Ethics

Epsilon is a preservation and interoperability project.

This repository does **not** aim to:

- redistribute proprietary client assets without review
- normalize cracked or malware-risk archive material
- present leaked or low-trust sources as production dependencies
- impersonate or misrepresent affiliation with Sulake or Habbo

When archive material is studied, it is treated as reference evidence for behavior,
formats, and compatibility, not as inherited runtime code.

## Current Technical Direction

- Runtime: `.NET 10`
- Architecture: modular monolith
- Persistence: PostgreSQL-backed read model with replaceable providers
- Cache / transient infra: Redis
- Admin surface: ASP.NET Core
- Protocol definition: external packet and command manifests
- Content strategy: importer pipelines and versioned manifests

## Current Status

The repository is beyond scaffold stage and already contains real implementation work:

- protocol packet and command registries
- room entry state machine
- room runtime snapshots and mutable room interaction services
- session/bootstrap surfaces
- housekeeping and support slices
- asset and package import pipelines
- centralized package management on current stable .NET packages

Recent hardening already addressed:

- forged in-room identity for movement and chat
- command execution without capability checks
- room runtime concurrency issues
- unsanitized `sign` / `carry` payloads
- missing chat flood control
- basic actor and furni collision gaps

## Project Layout

- [`src/`](src/) — production code
- [`tests/`](tests/) — automated tests
- [`docs/architecture/`](docs/architecture/) — architecture and domain design
- [`docs/compatibility/`](docs/compatibility/) — protocol and target-client material
- [`docs/decisions/`](docs/decisions/) — architectural decisions
- [`docs/roadmap/`](docs/roadmap/) — execution roadmap
- [`catalog/`](catalog/) — schemas and generated manifests
- [`tools/`](tools/) — importers and supporting tooling
- [`research/`](research/) — analysis, never runtime dependency

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

## Local Development

### Requirements

- `.NET SDK 10`
- Docker or Docker Desktop

### Infrastructure

The repository includes a local development stack in [`compose.yaml`](compose.yaml):

- PostgreSQL `16`
- Redis `7`

Environment defaults live in [`.env.example`](.env.example).

### Typical Flow

```bash
cp .env.example .env
docker compose up -d
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Epsilon.Gateway/Epsilon.Gateway.csproj
```

### Health Check

Once the gateway is running:

```bash
curl http://127.0.0.1:5000/health
```

## Security Position

Epsilon is still in alpha. The project already compiles and runs, but the security
surface is still being tightened as gameplay becomes more complete.

Near-term priorities include:

- replacing development-only authentication
- reducing open diagnostic and read surfaces
- binding all mutable actions to authenticated sessions
- moving more mutable runtime state off in-memory storage
- enforcing authorization consistently across gateway and services

## Non-Goals

- cloning a specific legacy emulator architecture
- copying legacy SQL schemas as the core domain
- embedding compatibility rules directly into gameplay code
- mixing research artifacts into runtime execution
- building the project around Adobe Flash as a runtime dependency

## Roadmap

The next major milestones are:

1. complete authenticated session flow and remove development auth assumptions
2. wire more gameplay actions through the protocol layer
3. implement inventory and furni mutation with anti-duplication guarantees
4. expand persistence beyond the current read-heavy slices
5. continue content extraction pipelines for public rooms, icons, packages, and client assets

## Disclaimer

Epsilon Emulator is an independent software project. It is not affiliated with,
endorsed by, or represented as an official Habbo or Sulake product.
