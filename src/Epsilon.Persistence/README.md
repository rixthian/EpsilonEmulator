# Epsilon.Persistence

This module should own:

- PostgreSQL repositories
- migrations
- import pipelines
- audit and replay storage

It should never leak table design into packet handling.

Current providers:

- `InMemory` for deterministic local slices
- `Postgres` for the modern read model defined in `/sql/001_hotel_read_model.sql`
