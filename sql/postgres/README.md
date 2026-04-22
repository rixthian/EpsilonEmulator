# PostgreSQL Schema Layout

The PostgreSQL schema is split by hotel domain so each migration stays explicit and reviewable.

## Files

- `010_identity.sql`
- `020_avatar.sql`
- `030_social.sql`
- `040_rooms.sql`
- `050_catalog.sql`
- `060_media.sql`
- `070_games.sql`
- `080_staff.sql`

## Guidance

- keep foreign keys and checks close to the owning domain
- add indexes for ownership, lookup, and room/session joins
- prefer append-only ledgers for balances, moderation events, and game scoring
- keep runtime-only data out of PostgreSQL unless it needs durable audit history

The older `001_hotel_read_model.sql` and `002_hotel_read_seed.sql` remain the first read-model slice. The files in this folder define the fuller hotel schema direction.
