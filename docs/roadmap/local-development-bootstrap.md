# Local Development Bootstrap

This document defines the minimum local runtime for Epsilon Emulator development.

## Required Local Services

- PostgreSQL
- Redis

These are provided through [compose.yaml](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/compose.yaml).

## First-Time Setup

1. Copy `.env.example` to `.env`
2. Adjust local ports or credentials if needed
3. Start dependencies:

```bash
docker compose up -d
```

## Intended Service Ports

- gateway http health: `8080`
- admin api http health: `8081`
- postgresql: `5432`
- redis: `6379`

## Current State

At this stage the runtime is still a foundation slice:

- configuration and manifests are environment-driven
- local infrastructure can be started reproducibly
- application services are not yet feature-complete

## Next Implementation Steps

- add persistence configuration and connection validation
- add redis configuration and cache abstraction
- add structured startup validation
- add auth/session vertical slice

