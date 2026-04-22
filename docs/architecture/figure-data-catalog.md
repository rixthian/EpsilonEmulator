# Figure Data Catalog

Epsilon treats `figuredata.xml` as its own canonical content surface.

It is related to avatar asset bundles, but it is not the same thing.

## Why

`figuredata.xml` defines:

- set types
- wearable sets
- part composition
- palette linkage
- gender and club availability

Avatar SWF bundles define the render/runtime assets that may support those figure definitions.

Those two sources should be connected, but they must not be collapsed into one file or one schema.

## Canonical Manifest

The canonical schema is:

- [figure-data-manifest.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/figure-data-manifest.schema.json)

It records:

- set type inventory
- set and part composition
- palette and color definitions
- part-type counts
- optional avatar-bundle context

## Tooling

```bash
python3 tools/importers/figure_data_manifest_builder.py \
  /path/to/figuredata.xml \
  --manifest-key production-2026-figuredata \
  --era-key production-2026 \
  --avatar-bundle catalog/avatar-assets/production-2026/flash-production-202604081915-644373475.manifest.json \
  --output catalog/figure-data/production-2026/production-2026-figuredata.manifest.json
```

## Design Rule

`figuredata.xml` is the structural definition of avatar composition.

Avatar asset bundles are render/runtime content families.

Epsilon uses both, but it catalogs them independently so compatibility work stays explicit.
