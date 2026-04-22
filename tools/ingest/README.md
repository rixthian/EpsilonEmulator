# Ingest Tooling

This folder hosts Epsilon-owned collection tooling.

Purpose:

- inventory raw asset roots
- classify files into canonical artifact groups
- preserve source profile metadata
- hand normalized outputs to importer stages

The ingest layer is not runtime code.

It sits before:

- item-definition import
- launcher profile import
- client-root inventory
- public-room inventory

Current entry point:

- `collect_assets.py`

That tool reads an ingest profile and emits a canonical asset-collection manifest.
