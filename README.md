<<<<<<< HEAD
# EpsilonEmulator
Emulador clasico
=======
# Epsilon Emulator

Epsilon Emulator is a modern, compatibility-first Habbo emulator project built from public reference material and legacy open-source research, without inheriting old emulator architecture as its foundation.

The mission is simple:

- preserve classic client behavior
- replace fragile legacy runtime assumptions
- document uncertainty instead of guessing
- build a modern, testable emulator that can survive long term
- hardcode as little game knowledge as possible

## Project Principles

- Legacy emulators are reference material, not base code.
- Original client behavior beats emulator folklore.
- Compatibility targets are versioned and explicit.
- Internal architecture is modern even when the external protocol is weird.
- Every inference should be traceable to a source.
- Protocol, content, and compatibility rules should be data-driven.
- Infrastructure should be replaceable without rewriting gameplay.
- Ten-year survivability matters more than quick imitation.

## Initial Direction

- Runtime: `.NET 8`
- Architecture: modular monolith
- Database: `PostgreSQL`
- Cache: `Redis`
- Admin/API: `ASP.NET Core`
- Target compatibility family: Flash `RELEASE63`

## Repository Map

- [docs/architecture/overview.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/overview.md)
- [docs/architecture/modules.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/modules.md)
- [docs/architecture/design-principles.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/design-principles.md)
- [docs/architecture/configuration-strategy.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/configuration-strategy.md)
- [docs/compatibility/target-client.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/compatibility/target-client.md)
- [docs/reference-sources/cataloging-rules.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/cataloging-rules.md)
- [docs/roadmap/phase-01.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/roadmap/phase-01.md)
- [catalog/source-catalog.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/source-catalog.schema.json)
- [catalog/source-catalog.seed.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/source-catalog.seed.json)

## Scope For V1

The first shippable milestone should support:

- handshake and authentication
- character selection
- room entry and actor presence
- movement and chat
- basic inventory and item placement
- basic navigator

More advanced systems such as pets, quests, effects, trading, wired, and minigames should follow only after protocol stability and room simulation correctness are proven.

## Non-Goals

- cloning a specific legacy emulator architecture
- shipping with copied legacy SQL schemas as the core model
- bundling questionable proprietary assets without provenance review
- mixing many client revisions into one unstable first release
- embedding packet ids, content metadata, or hotel rules directly in code when they can be loaded from versioned manifests

## Status

This repository is currently a foundation scaffold:

- architecture docs are in place
- source cataloging rules are defined
- module skeletons exist for implementation
- first code is focused on protocol and runtime boundaries
- protocol registration is being moved to manifest-driven configuration
>>>>>>> 2951a13 (Initial scaffold for Epsilon Emulator)
