# Visual Asset Pipeline

Epsilon needs a modern visual pipeline for:

- client package inventory
- avatar imaging and identity thumbnails
- furni and room imagery
- UI icons and static hotel visuals

## Layers

1. `visual asset manifest`
   Inventories extracted or already-available images such as `png`, `gif`, `jpg`, `ico`, and `webp`.

2. `client package manifest`
   Connects runtime package identities to feature flags and asset families.

3. `avatar imaging service`
   Renders derived avatar images for CMS, launcher, admin, moderation, and social surfaces from validated figure data and normalized avatar assets.

## Current Tools

- [visual_asset_manifest_builder.py](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/tools/importers/visual_asset_manifest_builder.py)
- [client_asset_root_manifest_builder.py](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/tools/importers/client_asset_root_manifest_builder.py)

## Reference Sources

- [nitro-imager.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/reference-sources/nitro-imager.md)
- [avatar-imaging-service.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/avatar-imaging-service.md)

## Runtime Rule

The runtime must depend only on canonical manifest keys.

It must never depend directly on old emulator directory names or archive layouts.

Derived images are cache outputs. They must never become authoritative account, inventory, effect, or room state.
