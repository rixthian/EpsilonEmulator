# PostgreSQL Read Model

The first PostgreSQL schema for Epsilon supports the initial hotel read slice. It is no longer the full database direction by itself.

Schema file:

- [001_hotel_read_model.sql](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/sql/001_hotel_read_model.sql)
- [002_hotel_read_seed.sql](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/sql/002_hotel_read_seed.sql)
- [sql/postgres/README.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/sql/postgres/README.md)

## Purpose

This schema is the first persistence target for:

- character profile loading
- subscription loading
- pet loading
- room loading
- room layout loading
- room item loading
- item definition loading

## Design Notes

- `accounts` and `characters` are split
- `room_layouts` are separate from `rooms`
- `item_definitions` are separate from `room_items`
- tags and public object sets use `JSONB` because they are variable-length lists
- foreign keys exist from the start
- this schema is intentionally smaller and stricter than legacy emulator dumps

## Direction

The files above define the first read model. The fuller hotel schema now expands by domain under `sql/postgres/`.

That split exists so Epsilon can store:

- identity and sessions
- avatar, clothing, effects, badges, achievements
- rooms, rights, ratings, public-room entries
- wallet, catalog, vouchers, ecotron, inventory
- sound, trax, jukebox
- games and minigames
- staff roles, moderation, advertisement campaigns

## Current Status

PostgreSQL-backed repositories exist for the first read slice in `src/Epsilon.Persistence`.

The remaining work is:

- wire the rest of the repositories to the domain schema
- add migrations beyond the initial read slice
- switch live features from `InMemory` to PostgreSQL or Redis as appropriate
- validate the gateway and admin APIs against the expanded schema
