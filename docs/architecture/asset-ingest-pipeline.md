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

## Reference Source: Arcturus Morningstar Default SWF Pack

Epsilon now has a reference-only ingest profile for the Arcturus Morningstar default SWF pack:

- [arcturus-morningstar-default-swf-pack.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-morningstar-default-swf-pack.md)
- [arcturus-morningstar-default-swf-pack.ingest-profile.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-morningstar-default-swf-pack.ingest-profile.json)
- [arcturus-morningstar-default-swf-pack.collection.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-morningstar-default-swf-pack.collection.json)
- [arcturus-morningstar-default-swf-pack.client-root.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-morningstar-default-swf-pack.client-root.json)
- [arcturus-morningstar-default-swf-pack.technical-summary.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/arcturus-morningstar-default-swf-pack.technical-summary.json)

This source is allowed to influence importers, schema design, taxonomy decisions, and tests. It is not allowed to become a launcher/client/runtime asset root without a separate normalization and provenance step.

## Catalog Registration

The same source is also registered in the canonical catalog so engineers can reference one stable source identity across ingest, client build analysis, launcher profile analysis, and client root analysis:

- [catalog ingest profile](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/ingest-profiles/arcturus-morningstar-default-swf-pack.manifest.json)
- [client build manifest](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/client-builds/flash-classic/arcturus-morningstar-default-swf-pack-habbo.manifest.json)
- [client root manifest](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/client-roots/arcturus-morningstar-default-swf-pack.manifest.json)
- [launcher profile manifest](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/launcher-profiles/arcturus-morningstar-default-swf-pack.manifest.json)

The launcher profile manifest may contain legacy remote URLs or local development URLs inherited from `external_variables.txt`. Treat those values as import evidence only. Runtime launch profiles must use rewritten, signed, Epsilon-owned asset URLs.

## Related Imaging Reference

`nitro-imager` is a separate reference for derived avatar image generation. It should inform the avatar imaging service and cache design, not the CMS or launcher runtime directly:

- [nitro-imager.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/nitro-imager.md)
- [avatar-imaging-service.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/avatar-imaging-service.md)
