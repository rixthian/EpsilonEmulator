# Runtime Network Topology

For the broader emulator authority model, see [emulator-reference-model.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/emulator-reference-model.md).

For the local multiprocess topology derived from reference stacks, see [local-orchestration-topology.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/local-orchestration-topology.md).

Epsilon has three runtime-facing application surfaces:

- `Gateway`
  hotel runtime, realtime session transport, and mutable gameplay actions
- `Launcher`
  client bootstrap and capability negotiation
- `Admin API`
  operational and privileged runtime inspection

## Network Plane Split

Epsilon separates transport into two planes:

- control plane
  - `HTTPS`
  - launcher bootstrap
  - registration and login
  - diagnostics and admin traffic
- realtime plane
  - `wss://`
  - hotel session commands
  - room join, move, and chat
  - future game and inventory realtime commands

The hotel runtime should not depend on plaintext `ws://` or `http://` for production gameplay traffic.

Loopback-only insecure realtime is allowed for local development when explicitly configured.

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
- TLS-protected realtime session traffic behind the gateway or reverse proxy

## Design Rule

Redis is the shared hot-state layer.

PostgreSQL remains the durable system of record.

In-memory state remains acceptable only as:

- deterministic local development storage
- single-process fallback
- per-node acceleration that can be rebuilt

## Transport Rule

Gameplay authority should flow through the realtime plane first.

HTTP protocol execution remains only as:

- fallback tooling for diagnostics
- controlled local integration support
- a bridge while client transports are being migrated

The target shape is:

- launcher/bootstrap over `HTTPS`
- hotel runtime over `wss://`
- no public plaintext gameplay transport
