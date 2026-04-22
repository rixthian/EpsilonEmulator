# Client Build Catalog

Epsilon separates three related but different concepts:

- `client build`
  one concrete client binary revision, usually one `Habbo.swf`
- `client root`
  one asset/content tree that a client build can consume
- `launcher profile`
  one runtime policy that tells Epsilon how to negotiate and bootstrap a client family

## Why This Split Exists

Classic hotel datasets often blur these concerns together. One folder can contain:

- one or more client SWFs
- room content
- furni runtime packages
- gamedata
- images
- public rooms

That is useful as a distribution artifact, but it is too loose for emulator architecture.

Epsilon keeps them separate because the same `client root` can contain:

- multiple SWF revisions
- mixed-era assets
- alternate package roots

and one `client build` can be paired with different launcher policies depending on capability and transport requirements.

## Independent SWF Categories

Each SWF revision must have its own independent category entry in the catalog.

That means:

- a build is not inferred only from a file name in a content root
- a build has its own manifest
- a build carries its own container metadata and runtime-surface summary
- a build can be mapped to launcher profiles and client roots without collapsing those into one file

## Client Build Manifest

The canonical manifest is defined in:

- [client-build-manifest.schema.json](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/catalog/client-build-manifest.schema.json)

It records:

- build identity
- client family
- era key
- SWF container signature and version
- frame and stage metadata
- rendering density summary
- top tag inventory
- high-level runtime surfaces such as:
  - guide
  - call for help
  - messenger
  - inventory effects
  - camera
  - wired
  - games
  - floorplan editor
  - avatar render

## Tooling

Use the builder to create build manifests from concrete SWF files:

```bash
python3 tools/importers/client_build_manifest_builder.py \
  /path/to/Habbo.swf \
  --build-key release63-201412101357-983561993 \
  --client-family flash-classic \
  --era-key release63 \
  --output catalog/client-builds/flash-classic/release63-201412101357-983561993.manifest.json
```

## Versioning

Client builds are versioned independently from notation and content specifications.

- build identity tracks the concrete binary revision
- manifest version tracks the Epsilon catalog format
- launcher profile version tracks runtime policy
- client root version tracks the asset/content tree shape

These must not be collapsed into a single version number.
