# Catalog Adaptation Strategy

Epsilon's catalog can be broader than any single historical client version.

That is valid, but it requires an adaptation layer.

Staff editing rules and operational safety are defined in [catalog-admin-tooling.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/catalog-admin-tooling.md). Visual catalog editors are useful, but the canonical catalog must be mutated through Epsilon admin commands rather than direct database tools.

## Canonical Catalog

The canonical catalog should support:

- nested page structure
- offers and bundles
- effects
- collectibles
- vouchers
- ecotron rewards
- seasonal campaigns
- rare-of-the-week style feature state

## Compatibility Rule

Not every client family can render every canonical catalog surface.

Protocol and client adapters should down-project the canonical catalog into the subset each client understands.

## Example

The canonical offer might include:

- bundle composition
- campaign metadata
- collectible flags
- modern preview metadata

An older client-facing adapter may only expose:

- page id
- offer id
- price
- item list

## Why This Matters

This lets Epsilon:

- preserve older compatibility targets
- support richer future portal and client experiences
- avoid building separate hotel catalogs per era
- protect catalog changes with draft, publish, audit, and rollback workflows
