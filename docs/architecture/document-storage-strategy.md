# Document Storage Strategy

Document storage is part of Epsilon's persistence topology, but it is not the primary store for the whole hotel.

## When Documents Fit

Document storage is useful for hotel surfaces that are naturally aggregate-shaped and change often:

- client package manifests
- advertisement campaigns and creative payloads
- housekeeping role catalogs and policy bundles
- localized content manifests
- room visual scene manifests
- analytics envelopes and import snapshots

These surfaces benefit from schema versioning, flexible payloads, and denormalized reads.

## What Should Not Move To Documents

The following concerns need strong transactional guarantees and relational integrity:

- accounts and characters
- wallet balances and ledgers
- inventory ownership
- room membership and item placement
- trading and voucher redemption
- permissions and moderation actions
- game sessions and scoring

Those belong in PostgreSQL.

## Redis Remains Separate

Redis is still the right place for hot ephemeral state:

- live room presence
- actor movement buffers
- flood control
- transient matchmaking queues
- short-lived session lookups

Redis should not become the source of truth for hotel ownership or economy.

## Document Ground Rules

When Epsilon uses document storage, it still requires:

- explicit repository contracts
- versioned document schemas
- deterministic identifiers
- bounded aggregate ownership
- import/export contracts

## Constraint

Document storage is a complement, not an excuse to blur domain boundaries.

If a surface needs cross-aggregate consistency, ledger guarantees, or strict permissions, keep it relational.
