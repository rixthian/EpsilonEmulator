# Platform Roadmap

## Vision

Epsilon is being built as a long-lived hotel platform that can:

- support multiple Habbo compatibility families
- preserve classic hotel behavior with stronger engineering discipline
- introduce modern infrastructure and security
- create room for original features such as roleplay systems and new gameplay surfaces

This roadmap is deliberately staged.

The project should not claim “all versions” through hacks.
It should earn multi-version support through adapter boundaries and stable core domains.

## Execution principles

1. one compatibility family must become trustworthy before the next one is added
2. protocol differences belong in adapters, not in hotel services
3. content visibility and launcher behavior must be version-aware
4. persistence, security, and operability are part of feature completion
5. original Epsilon features must sit above the compatibility baseline, not corrupt it

## Roadmap phases

### Phase 01: Stable vertical slice

Goal:

- deliver one trustworthy Flash-era hotel slice around `RELEASE63`

Focus:

- session/auth flow
- room entry, movement, and chat
- inventory and basic furni mutation
- protocol manifests and command execution
- health, admin diagnostics, and baseline persistence

Primary success signal:

- one adapter family behaves consistently and is testable end to end

### Phase 02: Operational hardening

Goal:

- make the emulator safe to run as a real service, not only as a development slice

Focus:

- shared session storage
- shared room presence/runtime coordination
- stronger authorization and moderation enforcement
- anti-duplication protections around economy and inventory
- logging, diagnostics, and runtime observability

Primary success signal:

- multi-process runtime behaves predictably under load and failure

### Phase 03: Full hotel feature completion

Goal:

- complete the major hotel systems expected from a serious emulator

Focus:

- navigator depth
- messenger and social flows
- groups and forums
- achievements, club, effects, vouchers, ecotron
- public room interactions
- bots, pets, and richer moderation tooling

Primary success signal:

- the hotel feels complete at the product level, not just at the packet level

### Phase 04: Multi-version expansion

Goal:

- support additional Habbo compatibility families without destabilizing the core

Focus:

- new protocol adapters
- new launcher/bootstrap profiles
- content adaptation by family
- compatibility matrices for rooms, catalog, avatar, games, and UI features

Primary success signal:

- multiple families run through explicit adapters with shared core services

### Phase 05: Original Epsilon product layer

Goal:

- move beyond historical parity where it adds clear product value

Focus:

- roleplay systems
- richer staff/admin tooling
- configurable bot intelligence and scripted world behavior
- new social, event, and community systems
- future-safe client and transport surfaces

Primary success signal:

- Epsilon is not only compatible; it is a modern platform with its own identity

## Roadmap by track

### Core runtime

- finish protocol-driven gameplay execution
- remove remaining high-risk `InMemory` dependency from live flows
- complete furni placement, pickup, and trading-safe mutation
- deepen room movement/pathing and live game runtime loops

### Security

- replace development auth assumptions
- complete account/session enforcement
- strengthen moderation scope and audit history
- add stronger abuse controls and operational logging

### Infrastructure

- complete Redis-backed coordination where distributed state matters
- finish Postgres-backed repositories for key domains
- keep configuration service-aware and environment-aware

### Compatibility

- keep `RELEASE63` as the first stable baseline
- add later family adapters only after behavioral confidence is real
- maintain a compatibility matrix instead of scattering version checks

### Product evolution

- complete groups/forums/community foundations
- evolve bots from scripted actors into configurable service systems
- add roleplay-specific systems as opt-in product modules

## What “all versions” means technically

It does not mean:

- one code path with revision-specific `if` statements everywhere
- one packet table for every era mixed together
- one content graph with no family-aware visibility rules

It means:

- shared hotel domains
- version-aware adapters
- version-aware content policies
- stable operational infrastructure

That is the only maintainable way to support many eras responsibly.

## Current milestone: `0.4.0-alpha.5`

The project is now crossing an important boundary:

- the CMS is no longer just a technical access helper
- the launcher is no longer just a bootstrap/debug page
- the desktop launcher exists as a real app package

Current release outcome:

- authenticated CMS homepage/login/register flow exists
- launcher access code flow exists
- native macOS launcher package exists
- published client routing exists through launcher profiles

Current release risk:

- CMS and launcher app are still unstable and need another hardening pass
- the published loader is still `game-loader`, not the final Unity/Nitro client package
- the persistence story is still not strong enough for a durable public hotel

That means the next roadmap priority is not “more surfaces.”
It is:

1. harden CMS and launcher app behavior
2. finish durable persistence for access-critical flows
3. publish the real client package target
