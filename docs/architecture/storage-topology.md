# Storage Topology

Epsilon should use different stores for different guarantees.

## PostgreSQL

PostgreSQL is the system of record for transactional hotel state.

It owns:

- accounts and authentication state
- characters, looks, clothing, effects, badges, achievements
- rooms, layouts, room rights, room ratings
- item definitions, inventory, room items, trading
- catalog pages, offers, products, vouchers, ecotron
- permissions, staff roles, moderation, bans, support cases
- games, rounds, teams, scores, official venues
- music, sound assets, song disks, jukebox queues

This is where integrity, foreign keys, constraints, and auditability matter most.

## Redis

Redis is the hot runtime layer.

It owns:

- active session ticket lookups
- room presence and actor runtime state
- movement throttling and flood control
- live chat fan-out buffers
- matchmaking queues
- temporary game round caches

Redis can be rebuilt from authoritative stores. It should never be the only source for economy or ownership.

## Document Storage

Document storage is optional and targeted.

It fits:

- client package manifests
- advertisement campaign payloads
- localized UI dictionaries
- visual scene manifests
- import snapshots
- telemetry envelopes

Document storage should not replace PostgreSQL for hotel identity, permissions, or commerce.

## Asset Storage

Binary assets should stay outside the databases.

Use object storage or CDN-backed paths for:

- icons
- furni images
- user photos
- sound files
- public room packages
- rendered campaign creatives

The databases should store metadata and references, not the asset binaries themselves.

## Rule Of Use

- choose PostgreSQL when integrity and transactions matter
- choose Redis when latency and expiry matter
- choose document storage when the payload is aggregate-shaped and versioned
- choose object storage when the data is binary

This keeps the hotel complete without forcing every problem into one engine.
