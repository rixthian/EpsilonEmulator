# Runtime Network Topology

Epsilon has three runtime-facing application surfaces:

- `Gateway`
  hotel runtime and mutable gameplay actions
- `Launcher`
  client bootstrap and capability negotiation
- `Admin API`
  operational and privileged runtime inspection

## Shared Runtime Rule

Any state that must survive process boundaries must not live only in process-local memory.

That includes:

- session tickets used by both launcher and gateway
- room-runtime coordination observed by admin and gateway
- presence and occupancy signals shared across nodes

## Current Shared-State Topology

- room-runtime coordination
  - local fallback for single-process development
  - Redis-backed coordination when configured
- sessions
  - local in-memory fallback for single-process development
  - Redis-backed shared session store when configured

## Deployment Modes

### Local Single Process Style

Useful for development and tests.

- `Auth.AllowInMemorySessions = true`
- empty Redis connection string
- launcher and gateway session state will not be shared across separate processes

### Multi-Process Development Or Production-Like Mode

Required when launcher, gateway, and admin run as separate processes or nodes.

- `Auth.AllowInMemorySessions = false`
- `Auth.RedisConnectionString = ...`
- `Infrastructure.RedisConnectionString = ...`

This enables:

- shared launcher/gateway session tickets
- shared room-runtime coordination
- cleaner operational inspection from the admin surface

## Design Rule

Redis is the shared hot-state layer.

PostgreSQL remains the durable system of record.

In-memory state remains acceptable only as:

- deterministic local development storage
- single-process fallback
- per-node acceleration that can be rebuilt
