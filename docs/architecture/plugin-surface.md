# Plugin Surface

Epsilon needs extension points, but not a plugin free-for-all.

## Core Rule

Plugins may extend defined surfaces only.

Those surfaces are:

- launcher presentation and client bootstrap decoration
- room behaviors
- game behaviors
- housekeeping tools
- content import pipelines

Plugins must not bypass:

- session validation
- capability checks
- economy invariants
- inventory ownership
- room movement validation

## Why This Matters

Large emulator forks commonly rely on plugin systems for rapid feature growth.

That is useful for:

- custom game modes
- room events
- mention systems
- notification flows
- launcher integrations

It is dangerous when plugins are allowed to mutate hotel state without stable contracts.

## Epsilon Direction

The plugin boundary should be:

- explicit
- versioned
- capability-aware
- isolated from runtime persistence details

Plugins should consume application services and extension contracts, not raw repositories.

## Immediate Implication

Launcher adaptation for phones, tablets, and future runtimes should be extensible through launcher-facing contracts.

That keeps:

- profile selection
- layout policy
- client hints
- transport selection

out of the hotel core.
