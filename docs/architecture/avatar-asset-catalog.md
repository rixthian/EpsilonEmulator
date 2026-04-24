# Avatar Asset Catalog

Epsilon keeps avatar-focused Flash asset bundles separate from:

- client builds
- client roots
- furni runtime packages
- public-room packages

## Why

Modern Flash-era datasets often ship large flat bundles that mostly contain:

- avatar part SWFs
- effect and action SWFs
- pet and companion SWFs
- figure support files
- override text and variables

These bundles are important, but they are not full hotel roots.

## Canonical Manifest

The canonical manifest is defined in:

- [avatar-asset-manifest.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/avatar-asset-manifest.schema.json)

It records:

- bundle identity
- era key
- supporting gamedata files
- category counts
- SWF signature and version distribution
- one inventory entry per SWF asset

## Asset Categories

Epsilon currently classifies avatar-bundle SWFs into:

- `avatar_part`
- `figure_library`
- `pet_companion`
- `action_effect`

This keeps wearable assets separate from runtime animation/effect overlays and pet-like companion assets.

## Tooling

Use the builder to inventory an avatar asset bundle:

```bash
python3 tools/importers/avatar_asset_manifest_builder.py \
  /path/to/avatar-bundle \
  --bundle-key flash-production-202604081915-644373475 \
  --era-key production-2026 \
  --output catalog/avatar-assets/production-2026/flash-production-202604081915-644373475.manifest.json
```

## Design Rule

Avatar bundles are content families.

They must not be mistaken for:

- full client distributions
- launcher profiles
- room content roots

They can be mapped to those systems later, but their catalog identity stays independent.

## Imaging Relationship

Avatar imaging is a derived-output layer on top of this catalog. It may use avatar asset manifests to render profile thumbnails and launcher identity cards, but it must not own avatar state or wearable ownership.

See:

- [avatar-imaging-service.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/avatar-imaging-service.md)
- [nitro-imager.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/nitro-imager.md)
