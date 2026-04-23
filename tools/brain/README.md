# Asset Intelligence Brain

This folder hosts the Epsilon update-intelligence subsystem.

Purpose:

- watch public upstream hotel artifacts
- capture normalized metadata and hashes
- detect changes between source snapshots
- produce actionable import recommendations
- keep SWF tooling policy separate from gameplay runtime

This folder is intentionally outside the server runtime.

It is a control-plane subsystem, not a room/gameplay subsystem.

## What It Is For

Use the brain when you need:

- official gamedata change detection
- public asset-watch automation
- SWF/source inventory and diff reports
- operator-ready update candidates

## What It Is Not For

This subsystem does not:

- bypass encryption or DRM
- extract keys
- emulate Widevine-like protection systems
- treat protected assets as automatically ingestible

If an upstream artifact is protected or requires a private key, the brain must mark it for manual review instead of trying to bypass it.

## Trusted Toolchain Direction

The companion `toolchain.template.json` models the recommended SWF toolchain:

- `JPEXS FFDec` for inspection/export
- `RABCDAsm` for low-level ABC patching
- `Apache Flex SDK` for clean compilation from source

These tools are part of an offline content pipeline.

They are not runtime dependencies.

## Entry Point

- `brain_cycle.py`

Supported subcommands:

- `snapshot`
- `diff`

## Example

Create a snapshot:

```bash
python3 tools/brain/brain_cycle.py snapshot \
  tools/brain/official_sources.template.json \
  /tmp/epsilon-source-snapshot.json
```

Compare two snapshots:

```bash
python3 tools/brain/brain_cycle.py diff \
  /tmp/epsilon-source-snapshot-old.json \
  /tmp/epsilon-source-snapshot.json \
  /tmp/epsilon-source-diff.json
```

## Recommended Integration Path

1. Run the brain on a schedule.
2. Persist snapshots and diffs as build artifacts.
3. Review update candidates in admin/operator tooling.
4. Trigger importer pipelines only after validation.

The correct order is:

- detect
- classify
- diff
- review
- import
- publish
