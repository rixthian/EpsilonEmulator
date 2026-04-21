# Document Storage Strategy

Epsilon should treat document storage as the primary persistence direction for mutable hotel aggregates.

## Why MongoDB Fits

MongoDB is a better fit than a heavily normalized relational schema for several Epsilon surfaces:

- character session bootstrap
- messenger rosters and request queues
- badge and achievement projections
- room runtime snapshots
- client package manifests
- housekeeping role definitions and capability catalogs
- advertisement campaigns

These are naturally document-shaped and evolve frequently.

## What Should Stay Structured

Using MongoDB does not mean abandoning structure.

Epsilon should still preserve:

- explicit repository contracts
- versioned document schemas
- stable aggregate boundaries
- deterministic identifiers
- append-style ledger/event records for balances and progression

## Aggregate Direction

Recommended primary document aggregates:

- `character_profiles`
- `character_sessions`
- `character_wallets`
- `character_social`
- `character_progression`
- `room_definitions`
- `room_runtime`
- `catalog_content`
- `client_packages`
- `housekeeping_roles`
- `advertisement_campaigns`

## Runtime Guidance

- Gateway and protocol layers must remain storage-agnostic.
- CoreGame and Rooms should depend only on repositories.
- Persistence can provide Mongo-backed repositories without changing domain services.
- Redis should remain available for hot ephemeral state, rate limiting, and fan-out coordination.

## Important Constraint

MongoDB is a good direction for document aggregates, but it is not an excuse for weak modeling.

If the aggregate boundaries are poor, moving from SQL to MongoDB only hides the problem inside larger blobs.
