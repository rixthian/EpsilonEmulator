# Design Principles

Epsilon Emulator should be built for change, not just for correctness on day one.

## Core Rules

### No Legacy Architecture Inheritance

Legacy emulators can teach behavior, packet flow, and feature scope, but they must not define Epsilon's internal shape.

### Minimize Hardcoded Knowledge

The following should live in versioned manifests, tables, or importable content formats whenever practical:

- packet ids
- packet names
- hotel texts and vars
- furni/product metadata
- content urls and revision metadata
- feature flags
- compatibility quirks

Hardcoding is acceptable only for true framework bootstrapping and carefully chosen defaults.

### Stable Domain, Replaceable Edges

Client protocols, storage engines, and deployment platforms may change over ten years. The domain model should not need to.

### Explicit Provenance

Every compatibility behavior should be explainable through:

- source evidence
- test evidence
- a concise architecture note when the decision affects runtime behavior

But source provenance should remain outside runtime code paths. Research is necessary; contamination of runtime files is not.

### Deterministic Simulation

Rooms and actor state should behave the same regardless of machine speed or infrastructure topology.

### Friendly By Design

The system should be understandable to maintainers:

- clear names
- small modules
- explicit contracts
- safe defaults
- operational visibility

## Ten-Year Test

Ask of every design choice:

- can this be reconfigured without a code fork
- can this be migrated without rewriting gameplay
- can another developer understand and replace this in five years
- does this make multi-era support easier instead of harder
