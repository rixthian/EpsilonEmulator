# Launcher Profile Model

Epsilon needs a neutral launcher-profile model for legacy variable packs such as
`external_variables.txt`.

These files are not just configuration. They describe what a client family
expects from the emulator and content pipeline.

## Core Rule

Epsilon should translate launcher variables into a canonical profile manifest.

The runtime should not depend directly on:

- old `%site_path%` placeholders
- literal legacy key names
- deployment-specific URLs
- era-specific campaign wiring

## Canonical Sections

A launcher profile manifest should classify:

- `content`
  - furnidata
  - productdata
  - image-library roots
  - external texts
  - figure data
- `downloads`
  - dynamic asset download endpoints
  - sample or media download roots
- `room`
  - room entry effects
  - moderation toggles
  - room-enter ad switches
- `catalog`
  - catalog feature flags
  - recycler and mystery-box settings
  - unique/limited toggles
- `games`
  - game-specific flags
  - scrolling/high-score behavior
- `community`
  - friend bar
  - social stream
  - event and feed surfaces
- `campaigns`
  - landing-view widgets
  - competitions
  - seasonal and quest campaign settings
- `moderation`
  - guide tool
  - moderator links and identity tooling

## Why This Matters

This lets Epsilon:

- compare client families cleanly
- derive launcher capability matrices
- externalize campaign and feature-state assumptions
- keep launcher behavior versioned and testable

## Design Consequence

Launcher variable packs belong in import tooling and manifests.

They should be translated into:

- launcher profile metadata
- capability sets
- content-source bindings
- feature-state defaults
