# Badge Assets

This directory is the managed local badge asset root for Epsilon.

- `library/` holds the synced badge image files used by development and import tooling.
- `mirrors.json` records the official mirror families that can be used as external fallbacks.

The `library/` directory is intentionally ignored by Git because the full badge set is large.
Use the badge import tool to regenerate metadata and optionally sync the local asset folder from a badge pack.
