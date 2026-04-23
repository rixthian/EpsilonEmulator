# Habbowood Revival

Epsilon should not copy legacy Habbowood implementations directly.

It should rebuild the feature as a clean event subsystem.

## What The Legacy Package Actually Contains

The `Femq/Ducketwood` package is useful because it preserves the structure of the old event:

- Flash runtime packages for actors, ambience, intro, sounds, and studio assets
- localized event text XML
- a minimal event backend for:
  - movie submission
  - leaderboard reads
  - vote writes
  - movie XML retrieval
- a browser wrapper with `flashobject` and `Ruffle`

The package is not a hotel-integrated subsystem.

It is a standalone event microsite with a small backend.

## Legacy Weaknesses

The legacy implementation is not acceptable as runtime truth.

Main problems:

- web-exposed SQLite storage model
- rate limiting tied only to hashed IP address
- raw XML movie payloads stored directly in the database
- no moderation workflow
- no account/session trust model
- no integration with hotel economy, identity, or permissions
- decoy `config.php` that is not operationally meaningful

## What Is Worth Preserving

The useful parts are:

- event flow
- content packaging shape
- submission and ranking model
- localized text surface
- actor/studio asset families
- idea of a creator-oriented seasonal event

## Correct Epsilon Rebuild

Habbowood should be rebuilt as:

- `EventDefinition`
- `EventSubmission`
- `EventVote`
- `EventLeaderboard`
- `EventAssetPackage`
- `EventModerationQueue`

The system should live above the hotel platform, not beside it.

## Domain Shape

Suggested boundaries:

- `Epsilon.Content`
  - event asset package manifests
  - localized event texts
  - actor/studio/sound package inventory
- `Epsilon.CoreGame`
  - event submission services
  - vote services
  - leaderboard services
  - eligibility, cooldown, and policy rules
- `Epsilon.Persistence`
  - durable event submissions
  - vote ledger
  - moderation review state
  - leaderboard projections
- `Epsilon.Gateway` / `Epsilon.AdminApi`
  - player and staff event endpoints

## Runtime Rules

The rebuilt feature should use:

- account or character identity, not only IP
- rate limits per user plus per-network heuristics
- append-only vote ledger
- moderation review for submissions
- canonical event payload schemas instead of raw arbitrary XML

## Asset Strategy

The legacy package clearly shows that Habbowood is an asset-heavy event.

Epsilon should treat it as:

- `EventAssetPackageDefinition`
- one package for stage/studio assets
- one package for actor packs
- one package for ambience
- one package for sounds
- one package for localization

The SWFs are reference content, not runtime authority.

## Recommended Implementation Order

1. `HabbowoodEventDefinition`
2. `MovieSubmission` persistence model
3. `VoteLedger` with anti-spam policy
4. leaderboard projection
5. asset-package manifests for the preserved SWFs
6. moderation and publishing flow
7. optional modern web UI adapter

## Revival Principle

The goal is not to run an old Flash site unchanged.

The goal is to revive the classic event with:

- better trust boundaries
- cleaner storage
- better moderation
- versioned content packages
- compatibility with modern launcher and event surfaces
