# First Hotel Read Slice

This slice introduces the first coherent hotel read path in Epsilon.

## Implemented

- character profile repository contract
- subscription repository contract
- pet profile repository contract
- room repository contract
- room layout repository contract
- room item repository contract
- item definition repository contract
- hotel read application service
- in-memory seeded persistence implementation
- gateway endpoints for character and room snapshots

## Endpoints

- `GET /hotel/characters/{characterId}`
- `GET /hotel/rooms/{roomId}`

## Current Purpose

This slice proves the architecture can load a coherent hotel view through:

- application-layer contracts
- replaceable persistence implementations
- clean room/content/core boundaries

## Current Limitation

Persistence is still an in-memory seeded implementation. The next step is replacing the seeded persistence layer with PostgreSQL-backed repositories.

