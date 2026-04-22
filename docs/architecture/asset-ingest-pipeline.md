# Asset Ingest Pipeline

Epsilon separates asset collection from asset normalization.

## Stages

1. collect
   Read a source profile and inventory the raw root.

2. classify
   Group files into canonical artifact families such as:
   - gamedata
   - furnitures
   - badges
   - clothes
   - effects
   - public rooms
   - gordon
   - hotelview
   - promo
   - mp3
   - Habbo client binaries

3. normalize
   Feed the specific importers:
   - item definitions
   - launcher profiles
   - client-root manifests
   - public-room manifests

## Why This Exists

The runtime should never depend on:

- raw download roots
- domain-specific URL conventions
- one-off archive folder layouts

The ingest layer absorbs that volatility and turns it into canonical Epsilon data.

## Current Tooling

- [collect_assets.py](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/tools/ingest/collect_assets.py)
- [ingest-profile.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/ingest-profile.schema.json)
- [asset-collection-manifest.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/asset-collection-manifest.schema.json)

## Design Rule

Collection is allowed to know:

- file roots
- folder layouts
- source domains
- revision folders

Runtime services are not.
