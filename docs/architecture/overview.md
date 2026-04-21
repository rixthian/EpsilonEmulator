# Architecture Overview

Epsilon Emulator should begin as a modular monolith with strong internal boundaries. This keeps iteration fast while preventing the transport layer, persistence layer, and game simulation from collapsing into one giant legacy-style codebase.

## Design Goals

- exact client compatibility at the protocol edge
- modern internal domain model
- deterministic room simulation
- versioned compatibility adapters
- source-backed behavior decisions
- testability before feature breadth

## High-Level Layers

1. Edge
   Handles sockets, framing, encryption, throttling, and session lifecycle.
2. Compatibility
   Encodes and decodes version-specific packets and maps them into stable internal commands and events.
3. Domain
   Implements auth, rooms, inventory, catalog, navigation, moderation, messenger, and other game systems.
4. Infrastructure
   Persists state, serves content metadata, exposes admin APIs, and provides caching and observability.

## Why A Modular Monolith

The emulator needs tight coordination between packets, simulation, and persistence. Splitting too early into microservices would add operational cost without helping correctness. The real priority is clean boundaries:

- no SQL in packet handlers
- no socket knowledge in game rules
- no client-version assumptions inside the core domain
- no hidden side effects in room simulation

## Runtime Model

- `Gateway` accepts connections and owns transport concerns.
- `Protocol` maps raw messages to internal commands.
- `Auth` resolves sessions and identity.
- `CoreGame` hosts shared game application services.
- `Rooms` runs deterministic room state and ticks.
- `Content` supplies furni, texts, figure data, and product metadata.
- `Persistence` owns repositories and import/export.
- `AdminApi` exposes moderation and operational controls.

## Compatibility Strategy

The initial compatibility target is Flash `RELEASE63`. Later versions should be supported through additional protocol adapters, not by polluting the core domain with version switches.

