# Protocol Frame Primitives

Legacy hotel protocol references are still useful for one narrow purpose:

- framing
- header boundaries
- primitive integer encoding
- packet grouping

They are not useful as modern runtime architecture.

## What Still Matters

The stable ideas are:

- frame length is explicit
- packet header is explicit
- payload follows after framing metadata
- runtime parser should reject obviously invalid lengths early
- packet parsing should stay independent from domain handlers

That translates into Epsilon as:

- protocol family specific frame readers
- typed packet registry
- transport parser isolated from hotel logic
- capability-aware launcher profile deciding which protocol family is active

## What Does Not Carry Over

The following should stay in reference material only:

- client-era control characters as application design
- packet folklore as the main source of truth
- direct reuse of old parser or header tables

Those references are valuable only as compatibility evidence.

## Current Epsilon Direction

Epsilon already aligns with the correct modern shape:

- framed parsing
- packet registry
- command manifests
- startup self-checks

The remaining work is deeper runtime integration, not primitive framing design.
