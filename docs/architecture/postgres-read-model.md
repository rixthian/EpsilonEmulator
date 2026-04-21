# PostgreSQL Read Model

The first PostgreSQL schema for Epsilon should support the same hotel read slice that currently runs on the in-memory provider.

Schema file:

- [001_hotel_read_model.sql](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/sql/001_hotel_read_model.sql)
- [002_hotel_read_seed.sql](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/sql/002_hotel_read_seed.sql)

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

## Current Status

The PostgreSQL-backed repositories now exist in `src/Epsilon.Persistence` and target this schema directly.

The remaining work is operational:

- restore NuGet packages
- apply the schema and seed files
- switch `Infrastructure.Provider` to `Postgres`
- validate the `Gateway` endpoints against a live database
