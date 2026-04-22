# Launcher Surface

Epsilon needs a launcher that is adaptable across client families without moving launcher logic into the hotel runtime.

## Core Rule

The launcher is a separate application boundary.

It is responsible for:

- profile discovery
- package selection
- entry asset resolution
- locale-aware bootstrap
- session-aware client startup

It is not responsible for:

- room simulation
- catalog mutation
- gameplay commands
- hotel moderation state

## Why It Exists

Different client families need different startup contracts:

- compatibility clients need a classical bootstrap surface
- modern web runtimes need a cleaner API-oriented manifest
- future native renderers may need different transport and entry rules

The launcher absorbs those differences while the hotel runtime stays stable.

## Epsilon Launcher Contract

The launcher should expose:

- profile listing
- default profile bootstrap
- explicit profile bootstrap
- localized client startup data
- session-bound bootstrap when a valid launcher session exists

## Bootstrap Output

A launcher bootstrap snapshot should contain:

- client profile identity
- package manifest
- gateway base URL
- entry asset URL
- asset base URL
- supported interface languages
- current interface preference when authenticated
- endpoint map for the selected client family

## Design Consequence

Launcher profiles must be configuration-driven, not hardcoded into controllers.

That keeps Epsilon flexible across:

- historical compatibility packages
- modern web runtimes
- future dedicated renderers
