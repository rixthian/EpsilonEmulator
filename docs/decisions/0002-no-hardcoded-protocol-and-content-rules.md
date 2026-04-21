# ADR 0002: Prefer Manifest-Driven Protocol And Content Definitions

## Status

Accepted

## Context

Legacy emulators often embed packet ids, hotel texts, item metadata, and compatibility quirks directly in code. This makes them fast to start but expensive to maintain, especially when supporting multiple eras or content revisions.

Epsilon Emulator is intended to survive platform changes and source expansion over many years.

## Decision

Represent protocol and content definitions through versioned manifests and importer outputs wherever possible.

Examples:

- packet registries
- packet naming and direction metadata
- furni/product definitions
- content revision references
- compatibility feature toggles

## Exceptions

Some small bootstrapping defaults may remain in code when required to start the process safely, but they must be easy to override and should never encode hotel-specific knowledge.

## Consequences

Benefits:

- easier multi-version support
- less duplication across compatibility families
- safer upgrades and migrations
- better tooling opportunities

Costs:

- more up-front schema and tooling work
- stronger need for validation and fixture tests

