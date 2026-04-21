# Configuration Strategy

Configuration must be treated as a product surface, not an afterthought.

## What Should Be Configurable

### Infrastructure

- database connection details
- cache providers
- transport bindings
- observability sinks
- rate limits and timeouts

### Compatibility

- target client family
- packet manifest path
- content manifest path
- enabled protocol adapters
- compatibility feature flags

### Content

- texts/vars sources
- furnidata revisions
- badge/effect manifests
- figuredata revisions

## What Should Not Be Configurable

- core invariants of the domain model
- security-critical logic shortcuts
- hidden behavior changes with no provenance

## Recommended Shapes

- typed options for runtime config
- JSON or TOML manifests for protocol/content metadata
- versioned importer outputs for legacy source material
- explicit migration scripts for persistent schema changes

## Rule Of Replacement

If a future maintainer needs to swap:

- PostgreSQL for another relational database
- Redis for another cache
- one packet manifest for another client family
- one content snapshot for another revision

they should do so through configuration and adapters, not through business-logic surgery.

