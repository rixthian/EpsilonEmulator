# Public Room Asset Pipeline

## Goal

Convert legacy Flash public-room packages into a stable Epsilon content format that can survive renderer and runtime changes.

This is not a direct runtime dependency on `.swf`.

The pipeline must transform source packages into:

- a canonical manifest
- extracted symbol inventory
- normalized room identity
- renderer-independent metadata

## Source Reality

Flash public rooms are distributed as `.swf` bundles such as `hh_room_lobby.swf`, `hh_room_pool.swf`, `hh_room_theater.swf`, and many branded or locale-specific variants.

These bundles are Flash assets, not Shockwave/Director assets.

Local canonical inventory output is generated in:

- [release63-plus-public-rooms.manifest.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/public-rooms/release63-plus-public-rooms.manifest.json)

## Conversion Strategy

The conversion pipeline should be split into stages:

1. Inventory
   Scan a source directory and identify every public-room package.

2. Classification
   Separate canonical rooms from regional variants, campaign variants, and branded overlays.

3. Extraction
   Inspect each source package to enumerate container metadata, tag inventory, linkage tables, embedded bitmaps, sprite counts, labels, and other structural hints.

4. Normalization
   Produce a canonical room key, variant key, and asset record for each package.

5. Canonical Manifest Emission
   Emit a JSON manifest that Epsilon can load without knowing anything about source archive structure.

6. Renderer Adaptation
   Convert extracted visual assets into whichever runtime renderer Epsilon uses later.

## Important Constraint

There is no serious “convert all SWFs” shortcut.

What survives ten years is not the SWF itself. What survives is:

- normalized content identity
- extracted asset metadata
- stable manifests
- renderer adapters that can be replaced

## Canonical Model

Each public room package should map to:

- `roomKey`
- `variantKey`
- `sourceFilename`
- `assetFamily`
- `visualLayerMode`
- `brandTags`
- `localeTags`
- `sharedAssetDependencies`
- `conversionStatus`

## Initial Output

The first deliverable is a manifest inventory with optional SWF metadata extraction, not a perfect visual conversion.

That is the correct order because:

- it gives full coverage quickly
- it makes gaps visible
- it prevents ad hoc renderer coupling
- it allows later extraction passes without changing runtime contracts

## Implemented Now

The importer tooling now supports two levels:

- filename-based inventory
- SWF technical metadata extraction

Current extracted technical fields include:

- signature and SWF version
- declared file length
- frame size
- frame rate
- frame count
- tag counts by type
- exported assets
- symbol classes
- sprite count
- shape tag count
- bitmap tag count
- action tag count

That is enough to begin canonical grouping, renderer planning, and shared-asset analysis without binding Epsilon runtime to SWF.
