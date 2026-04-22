# Client Asset Root Model

Epsilon needs a neutral model for legacy client content roots.

These roots often mix:

- launcher/bootstrap metadata
- furnidata and productdata
- figure and avatar assets
- public-room packages
- furni SWFs
- ad and branded assets
- image libraries
- versioned package roots
- flat asset roots without `gamedata/`, `dcr/`, or `gordon/`

## Core Rule

Epsilon should treat a client asset root as an importable content layout, not as runtime structure.

The emulator core should never depend directly on:

- legacy folder names
- raw SWF trees
- old web deployment paths
- old package hashes

## Canonical Sections

A client root manifest should inventory these sections:

- `gamedata`
  - furnidata
  - productdata
  - figuredata
  - localized text
  - external variables
  - security cast files
- `imageLibrary`
  - badges
  - teasers
  - icons
  - hotel-view images
- `dcrFurnitureAssets`
  - furni SWFs or flat `hot_furni` libraries
  - branded/ad furni
  - game-specific furni
- `gordonPackages`
  - versioned package roots
- `coreClientPackages`
  - client binaries and room-content core files
- `publicRoomPackages`
  - official public-room SWFs and branded variants
- `avatarAssets`
  - human figure libraries
  - part variants
  - accessories
- `petAssets`
  - pet figures
  - palettes

## Why This Matters

This lets Epsilon support:

- multiple client families
- package adaptation by profile
- public-room and branded-room inventory
- ad/campaign surfaces
- avatar and clothing import pipelines
- future launcher/bootstrap compatibility matrices

## Design Consequence

Client content roots belong in import tooling and manifests.

They should be translated into:

- canonical content definitions
- package manifests
- launcher profile metadata
- localized bundles
- visual asset registries
